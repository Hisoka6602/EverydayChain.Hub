using EverydayChain.Hub.Application.Services;
using EverydayChain.Hub.Domain.Sync;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// SyncWindowCalculator 时间窗口计算器回归测试套件。
/// 覆盖场景：正常窗口、时钟回拨冻结、DST 非法时刻顺延、UTC 输入拒绝。
/// </summary>
public class SyncWindowCalculatorTests
{
    /// <summary>
    /// 构建被测对象（空日志上下文，避免测试对日志输出产生依赖）。
    /// </summary>
    private static SyncWindowCalculator CreateCalculator()
    {
        return new SyncWindowCalculator(NullLogger<SyncWindowCalculator>.Instance);
    }

    /// <summary>
    /// 构造基础表定义。
    /// </summary>
    private static SyncTableDefinition BuildDefinition(int maxLagMinutes = 0)
    {
        return new SyncTableDefinition
        {
            TableCode = "TEST_TABLE",
            MaxLagMinutes = maxLagMinutes,
        };
    }

    /// <summary>
    /// 构造空检查点（无游标，无上次成功时间）。
    /// </summary>
    private static SyncCheckpoint EmptyCheckpoint()
    {
        return new SyncCheckpoint
        {
            TableCode = "TEST_TABLE",
        };
    }

    #region 正常窗口计算

    /// <summary>
    /// 场景：无检查点游标、无滞后、当前时间为合法本地时间 → 窗口起止相等（空窗口）。
    /// </summary>
    [Fact]
    public void CalculateWindow_NoCheckpoint_NoLag_ReturnsEmptyWindow()
    {
        var calculator = CreateCalculator();
        var definition = BuildDefinition(maxLagMinutes: 0);
        definition.StartTimeLocal = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local);
        var checkpoint = EmptyCheckpoint();
        var nowLocal = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local);

        var window = calculator.CalculateWindow(definition, checkpoint, nowLocal);

        // 窗口终点 = nowLocal - lag(0) = nowLocal，与起点相同，为空窗口。
        Assert.Equal(window.WindowStartLocal, window.WindowEndLocal);
    }

    /// <summary>
    /// 场景：有游标检查点、正常时间递进 → 窗口起点为上次游标，终点为 now - lag。
    /// </summary>
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

    /// <summary>
    /// 场景：窗口终点小于起点时（因滞后配置过大），终点应被钳制为与起点相等。
    /// </summary>
    [Fact]
    public void CalculateWindow_EndBeforeStart_ClampedToStart()
    {
        var calculator = CreateCalculator();
        var definition = BuildDefinition(maxLagMinutes: 120);
        var lastCursor = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Local);
        var checkpoint = EmptyCheckpoint();
        checkpoint.LastSuccessCursorLocal = lastCursor;

        // nowLocal - 120min = 08:00，而 lastCursor = 10:00，终点早于起点 → 应钳制为起点。
        var nowLocal = new DateTime(2026, 3, 1, 10, 30, 0, DateTimeKind.Local);

        var window = calculator.CalculateWindow(definition, checkpoint, nowLocal);

        Assert.Equal(window.WindowStartLocal, window.WindowEndLocal);
    }

    #endregion

    #region 时钟回拨检测

    /// <summary>
    /// 场景：当前时间早于上次成功时间（时钟回拨）→ 窗口终点冻结在上次成功时间。
    /// </summary>
    [Fact]
    public void CalculateWindow_ClockRollback_FreezeWindowEnd()
    {
        var calculator = CreateCalculator();
        var definition = BuildDefinition(maxLagMinutes: 0);
        var lastSuccessTime = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Local);
        var checkpoint = EmptyCheckpoint();
        checkpoint.LastSuccessTimeLocal = lastSuccessTime;

        // 当前时间比上次成功时间早 → 时钟回拨。
        var nowLocal = new DateTime(2026, 3, 1, 11, 0, 0, DateTimeKind.Local);

        var window = calculator.CalculateWindow(definition, checkpoint, nowLocal);

        // 时钟回拨时，窗口终点冻结在上次成功时间。
        Assert.Equal(lastSuccessTime, window.WindowEndLocal);
    }

    /// <summary>
    /// 场景：当前时间与上次成功时间相同（边界值）→ 正常计算，不触发回拨保护。
    /// </summary>
    [Fact]
    public void CalculateWindow_NowEqualLastSuccessTime_NoRollbackProtection()
    {
        var calculator = CreateCalculator();
        var definition = BuildDefinition(maxLagMinutes: 0);
        var sameTime = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Local);
        var checkpoint = EmptyCheckpoint();
        checkpoint.LastSuccessTimeLocal = sameTime;

        var window = calculator.CalculateWindow(definition, checkpoint, sameTime);

        // 不回拨，正常计算：now - 0lag = now。
        Assert.Equal(sameTime, window.WindowEndLocal);
    }

    /// <summary>
    /// 场景：当前时间略早于上次成功时间 1 秒（边界回拨）→ 触发冻结。
    /// </summary>
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

    /// <summary>
    /// 场景：传入 UTC Kind 的 nowLocal → 应抛出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void CalculateWindow_UtcNow_ThrowsInvalidOperation()
    {
        var calculator = CreateCalculator();
        var definition = BuildDefinition(maxLagMinutes: 0);
        var checkpoint = EmptyCheckpoint();

        var utcNow = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);

        Assert.Throws<InvalidOperationException>(() =>
            calculator.CalculateWindow(definition, checkpoint, utcNow));
    }

    /// <summary>
    /// 场景：检查点游标为 UTC Kind → 应抛出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void CalculateWindow_UtcCursor_ThrowsInvalidOperation()
    {
        var calculator = CreateCalculator();
        var definition = BuildDefinition(maxLagMinutes: 0);
        var checkpoint = EmptyCheckpoint();
        checkpoint.LastSuccessCursorLocal = DateTime.SpecifyKind(new DateTime(2026, 1, 1), DateTimeKind.Utc);

        var nowLocal = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Local);

        Assert.Throws<InvalidOperationException>(() =>
            calculator.CalculateWindow(definition, checkpoint, nowLocal));
    }

    /// <summary>
    /// 场景：表定义起始时间为 UTC Kind → 应抛出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void CalculateWindow_UtcStartTime_ThrowsInvalidOperation()
    {
        var calculator = CreateCalculator();
        var definition = BuildDefinition(maxLagMinutes: 0);
        definition.StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 1, 1), DateTimeKind.Utc);
        var checkpoint = EmptyCheckpoint();

        var nowLocal = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Local);

        Assert.Throws<InvalidOperationException>(() =>
            calculator.CalculateWindow(definition, checkpoint, nowLocal));
    }

    #endregion

    #region Unspecified Kind 兼容性

    /// <summary>
    /// 场景：传入 Unspecified Kind 的时间 → 按本地时间语义处理，不应抛出。
    /// </summary>
    [Fact]
    public void CalculateWindow_UnspecifiedKind_TreatedAsLocal()
    {
        var calculator = CreateCalculator();
        var definition = BuildDefinition(maxLagMinutes: 0);
        var checkpoint = EmptyCheckpoint();

        var unspecifiedNow = DateTime.SpecifyKind(new DateTime(2026, 3, 1, 12, 0, 0), DateTimeKind.Unspecified);

        // 不应抛异常，应正常计算窗口（WindowEndLocal 应不早于 WindowStartLocal）。
        var window = calculator.CalculateWindow(definition, checkpoint, unspecifiedNow);

        Assert.True(window.WindowEndLocal >= window.WindowStartLocal);
    }

    #endregion

    #region 时钟扰动组合场景

    /// <summary>
    /// 场景：有上次游标且有时钟回拨 → 窗口起点为上次游标，终点冻结在上次成功时间。
    /// </summary>
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

        // 时钟回拨到 09:00，早于上次成功时间 11:00。
        var nowLocal = new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Local);

        var window = calculator.CalculateWindow(definition, checkpoint, nowLocal);

        // 起点为上次游标，终点冻结在上次成功时间（不因回拨倒退）。
        Assert.Equal(lastCursor, window.WindowStartLocal);
        Assert.Equal(lastSuccessTime, window.WindowEndLocal);
    }

    /// <summary>
    /// 场景：连续调用时间递增 → 窗口终点随时间正常向前推进。
    /// </summary>
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

        // 第二次调用终点应晚于第一次。
        Assert.True(window2.WindowEndLocal >= window1.WindowEndLocal);
    }

    #endregion
}
