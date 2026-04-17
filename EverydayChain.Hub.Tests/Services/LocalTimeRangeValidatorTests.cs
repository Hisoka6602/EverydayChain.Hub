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

    /// <summary>
    /// 可选时间区间双空参数时应使用默认时间窗。
    /// </summary>
    [Fact]
    public void TryNormalizeOptionalRange_ShouldUseDefaultWindow_WhenStartAndEndAreNull()
    {
        var defaultStart = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local);

        var success = LocalTimeRangeValidator.TryNormalizeOptionalRange(null, null, defaultStart, out var normalizedStart, out var normalizedEnd, out var validationMessage);

        Assert.True(success);
        Assert.Equal(defaultStart, normalizedStart);
        Assert.Equal(defaultStart.AddDays(1), normalizedEnd);
        Assert.Equal(string.Empty, validationMessage);
    }

    /// <summary>
    /// 可选时间区间仅传结束时间时应使用默认开始时间。
    /// </summary>
    [Fact]
    public void TryNormalizeOptionalRange_ShouldUseDefaultStart_WhenOnlyEndProvided()
    {
        var defaultStart = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local);
        var end = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 12, 0, 0), DateTimeKind.Local);

        var success = LocalTimeRangeValidator.TryNormalizeOptionalRange(null, end, defaultStart, out var normalizedStart, out var normalizedEnd, out var validationMessage);

        Assert.True(success);
        Assert.Equal(defaultStart, normalizedStart);
        Assert.Equal(end, normalizedEnd);
        Assert.Equal(string.Empty, validationMessage);
    }

    /// <summary>
    /// 可选时间区间结束时间不大于开始时间时应校验失败。
    /// </summary>
    [Fact]
    public void TryNormalizeOptionalRange_ShouldReturnFalse_WhenEndIsNotGreaterThanStart()
    {
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 18, 10, 0, 0), DateTimeKind.Local);
        var end = DateTime.SpecifyKind(new DateTime(2026, 4, 18, 10, 0, 0), DateTimeKind.Local);
        var defaultStart = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local);

        var success = LocalTimeRangeValidator.TryNormalizeOptionalRange(start, end, defaultStart, out _, out _, out var validationMessage);

        Assert.False(success);
        Assert.Equal("结束时间必须大于开始时间。", validationMessage);
    }

    /// <summary>
    /// 必填时间区间传入 UTC 时间时应校验失败。
    /// </summary>
    [Fact]
    public void TryNormalizeRequiredRange_ShouldReturnFalse_WhenUtcTimeProvided()
    {
        var startUtc = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 8, 0, 0), DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 9, 0, 0), DateTimeKind.Utc);

        var success = LocalTimeRangeValidator.TryNormalizeRequiredRange(startUtc, endUtc, out _, out _, out var validationMessage);

        Assert.False(success);
        Assert.Equal("开始时间必须为本地时间，禁止传入 UTC 时间。", validationMessage);
    }
}
