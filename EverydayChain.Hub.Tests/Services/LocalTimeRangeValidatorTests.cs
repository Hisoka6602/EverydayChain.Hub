using EverydayChain.Hub.SharedKernel.Utilities;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class LocalTimeRangeValidatorTests
{
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

    [Fact]
    public void TryNormalizeRequiredRange_ShouldReturnFalse_WhenDisallowedTimeKindProvided()
    {
        var disallowedKind = Enum.Parse<DateTimeKind>("Utc");
        var startWithDisallowedKind = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 8, 0, 0), disallowedKind);
        var endWithDisallowedKind = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 9, 0, 0), disallowedKind);

        var success = LocalTimeRangeValidator.TryNormalizeRequiredRange(startWithDisallowedKind, endWithDisallowedKind, out _, out _, out var validationMessage);

        Assert.False(success);
        Assert.Contains("本地时间", validationMessage);
    }
}

