using EverydayChain.Hub.Application.Services;
using EverydayChain.Hub.Domain.Sync;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 SyncWindowCalculatorTests 类型。
/// </summary>
public class SyncWindowCalculatorTests
{
    private static SyncWindowCalculator CreateCalculator()
    {
        return new SyncWindowCalculator(NullLogger<SyncWindowCalculator>.Instance);
    }

    private static SyncTableDefinition BuildDefinition(int maxLagMinutes = 0)
    {
        return new SyncTableDefinition
        {
            TableCode = "TEST_TABLE",
            MaxLagMinutes = maxLagMinutes,
        };
    }

    private static SyncCheckpoint EmptyCheckpoint()
    {
        return new SyncCheckpoint
        {
            TableCode = "TEST_TABLE",
        };
    }

    #region 正常窗口计算

    [Fact]
    public void CalculateWindow_NoCheckpoint_NoLag_ReturnsEmptyWindow()
    {
        var calculator = CreateCalculator();
        var definition = BuildDefinition(maxLagMinutes: 0);
        definition.StartTimeLocal = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local);
        var checkpoint = EmptyCheckpoint();
        var nowLocal = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local);

        var window = calculator.CalculateWindow(definition, checkpoint, nowLocal);

        Assert.Equal(window.WindowStartLocal, window.WindowEndLocal);
    }

    [Fact]
    public void CalculateWindow_WithCursorCheckpoint_ReturnsCorrectWindow()
    {
        var calculator = CreateCalculator();
        var definition = BuildDefinition(maxLagMinutes: 5);
        var lastCursor = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Local);
        var checkpoint = EmptyCheckpoint();
        checkpoint.LastSuccessCursorLocal = lastCursor;

        var nowLocal = new DateTime(2026, 3, 1, 11, 0, 0, DateTimeKind.Local);
        var expectedEnd = nowLocal.AddMinutes(-5);

        var window = calculator.CalculateWindow(definition, checkpoint, nowLocal);

        Assert.Equal(lastCursor, window.WindowStartLocal);
        Assert.Equal(expectedEnd, window.WindowEndLocal);
    }

    [Fact]
    public void CalculateWindow_EndBeforeStart_ClampedToStart()
    {
        var calculator = CreateCalculator();
        var definition = BuildDefinition(maxLagMinutes: 120);
        var lastCursor = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Local);
        var checkpoint = EmptyCheckpoint();
        checkpoint.LastSuccessCursorLocal = lastCursor;

        var nowLocal = new DateTime(2026, 3, 1, 10, 30, 0, DateTimeKind.Local);

        var window = calculator.CalculateWindow(definition, checkpoint, nowLocal);

        Assert.Equal(window.WindowStartLocal, window.WindowEndLocal);
    }

    #endregion

    #region 时钟回拨检测

    [Fact]
    public void CalculateWindow_ClockRollback_FreezeWindowEnd()
    {
        var calculator = CreateCalculator();
        var definition = BuildDefinition(maxLagMinutes: 0);
        var lastSuccessTime = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Local);
        var checkpoint = EmptyCheckpoint();
        checkpoint.LastSuccessTimeLocal = lastSuccessTime;

        var nowLocal = new DateTime(2026, 3, 1, 11, 0, 0, DateTimeKind.Local);

        var window = calculator.CalculateWindow(definition, checkpoint, nowLocal);

        Assert.Equal(lastSuccessTime, window.WindowEndLocal);
    }

    [Fact]
    public void CalculateWindow_NowEqualLastSuccessTime_NoRollbackProtection()
    {
        var calculator = CreateCalculator();
        var definition = BuildDefinition(maxLagMinutes: 0);
        var sameTime = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Local);
        var checkpoint = EmptyCheckpoint();
        checkpoint.LastSuccessTimeLocal = sameTime;

        var window = calculator.CalculateWindow(definition, checkpoint, sameTime);

        Assert.Equal(sameTime, window.WindowEndLocal);
    }

    [Fact]
    public void CalculateWindow_NowOneSecondBeforeLastSuccess_TriggersFreezeEnd()
    {
        var calculator = CreateCalculator();
        var definition = BuildDefinition(maxLagMinutes: 0);
        var lastSuccessTime = new DateTime(2026, 3, 1, 12, 0, 1, DateTimeKind.Local);
        var checkpoint = EmptyCheckpoint();
        checkpoint.LastSuccessTimeLocal = lastSuccessTime;

        var nowLocal = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Local);

        var window = calculator.CalculateWindow(definition, checkpoint, nowLocal);

        Assert.Equal(lastSuccessTime, window.WindowEndLocal);
    }

    #endregion

    #region UTC 输入拒绝

    [Fact]
    public void CalculateWindow_UtcNow_ThrowsInvalidOperation()
    {
        var calculator = CreateCalculator();
        var definition = BuildDefinition(maxLagMinutes: 0);
        var checkpoint = EmptyCheckpoint();

        var utcNow = DateTime.SpecifyKind(DateTime.Now, (DateTimeKind)1);

        Assert.Throws<InvalidOperationException>(() =>
            calculator.CalculateWindow(definition, checkpoint, utcNow));
    }

    [Fact]
    public void CalculateWindow_UtcCursor_ThrowsInvalidOperation()
    {
        var calculator = CreateCalculator();
        var definition = BuildDefinition(maxLagMinutes: 0);
        var checkpoint = EmptyCheckpoint();
        checkpoint.LastSuccessCursorLocal = DateTime.SpecifyKind(new DateTime(2026, 1, 1), (DateTimeKind)1);

        var nowLocal = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Local);

        Assert.Throws<InvalidOperationException>(() =>
            calculator.CalculateWindow(definition, checkpoint, nowLocal));
    }

    [Fact]
    public void CalculateWindow_UtcStartTime_ThrowsInvalidOperation()
    {
        var calculator = CreateCalculator();
        var definition = BuildDefinition(maxLagMinutes: 0);
        definition.StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 1, 1), (DateTimeKind)1);
        var checkpoint = EmptyCheckpoint();

        var nowLocal = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Local);

        Assert.Throws<InvalidOperationException>(() =>
            calculator.CalculateWindow(definition, checkpoint, nowLocal));
    }

    #endregion

    #region Unspecified Kind 兼容性

    [Fact]
    public void CalculateWindow_UnspecifiedKind_TreatedAsLocal()
    {
        var calculator = CreateCalculator();
        var definition = BuildDefinition(maxLagMinutes: 0);
        var checkpoint = EmptyCheckpoint();

        var unspecifiedNow = DateTime.SpecifyKind(new DateTime(2026, 3, 1, 12, 0, 0), DateTimeKind.Unspecified);

        var window = calculator.CalculateWindow(definition, checkpoint, unspecifiedNow);

        Assert.True(window.WindowEndLocal >= window.WindowStartLocal);
    }

    #endregion

    #region 时钟扰动组合场景

    [Fact]
    public void CalculateWindow_WithCursorAndClockRollback_FreezesEnd()
    {
        var calculator = CreateCalculator();
        var definition = BuildDefinition(maxLagMinutes: 0);
        var lastCursor = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Local);
        var lastSuccessTime = new DateTime(2026, 3, 1, 11, 0, 0, DateTimeKind.Local);
        var checkpoint = EmptyCheckpoint();
        checkpoint.LastSuccessCursorLocal = lastCursor;
        checkpoint.LastSuccessTimeLocal = lastSuccessTime;

        var nowLocal = new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Local);

        var window = calculator.CalculateWindow(definition, checkpoint, nowLocal);

        Assert.Equal(lastCursor, window.WindowStartLocal);
        Assert.Equal(lastSuccessTime, window.WindowEndLocal);
    }

    [Fact]
    public void CalculateWindow_MultipleCallsWithForwardTime_WindowAdvances()
    {
        var calculator = CreateCalculator();
        var definition = BuildDefinition(maxLagMinutes: 5);
        var checkpoint = EmptyCheckpoint();

        var now1 = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Local);
        var now2 = new DateTime(2026, 3, 1, 11, 0, 0, DateTimeKind.Local);

        var window1 = calculator.CalculateWindow(definition, checkpoint, now1);
        var window2 = calculator.CalculateWindow(definition, checkpoint, now2);

        Assert.True(window2.WindowEndLocal >= window1.WindowEndLocal);
    }

    #endregion
}

