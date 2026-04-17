using EverydayChain.Hub.SharedKernel.Utilities;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 本地时间区间校验工具测试。
/// </summary>
public sealed class LocalTimeRangeValidatorTests
{
    /// <summary>
    /// 必填时间区间校验通过时应返回规范化时间。
    /// </summary>
    [Fact]
    public void TryNormalizeRequiredRange_ShouldReturnTrue_WhenRangeIsValid()
    {
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 8, 0, 0), DateTimeKind.Local);
        var end = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 9, 0, 0), DateTimeKind.Local);

        var success = LocalTimeRangeValidator.TryNormalizeRequiredRange(start, end, out var normalizedStart, out var normalizedEnd, out var validationMessage);

        Assert.True(success);
        Assert.Equal(start, normalizedStart);
        Assert.Equal(end, normalizedEnd);
        Assert.Equal(string.Empty, validationMessage);
    }

    /// <summary>
    /// 可选时间区间仅传开始时间时应默认结束时间为开始时间加一天。
    /// </summary>
    [Fact]
    public void TryNormalizeOptionalRange_ShouldDefaultEndToStartPlusOneDay_WhenOnlyStartProvided()
    {
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 18, 10, 0, 0), DateTimeKind.Local);
        var defaultStart = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local);

        var success = LocalTimeRangeValidator.TryNormalizeOptionalRange(start, null, defaultStart, out var normalizedStart, out var normalizedEnd, out var validationMessage);

        Assert.True(success);
        Assert.Equal(start, normalizedStart);
        Assert.Equal(start.AddDays(1), normalizedEnd);
        Assert.Equal(string.Empty, validationMessage);
    }
}
