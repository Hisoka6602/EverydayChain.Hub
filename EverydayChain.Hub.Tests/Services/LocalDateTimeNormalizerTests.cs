using EverydayChain.Hub.SharedKernel.Utilities;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 本地时间规范化工具测试。
/// </summary>
public sealed class LocalDateTimeNormalizerTests {
    /// <summary>
    /// 非本地时间语义应被拒绝。
    /// </summary>
    [Fact]
    public void TryNormalize_ShouldRejectNonLocalKind() {
        var input = DateTime.SpecifyKind(new DateTime(2026, 4, 13, 12, 0, 0), (DateTimeKind)1);

        var passed = LocalDateTimeNormalizer.TryNormalize(input, "仅支持本地时间语义", out _, out var message);

        Assert.False(passed);
        Assert.Equal("仅支持本地时间语义", message);
    }

    /// <summary>
    /// Unspecified 时间应转为 Local。
    /// </summary>
    [Fact]
    public void TryNormalize_ShouldConvertUnspecifiedToLocal() {
        var input = new DateTime(2026, 4, 13, 12, 0, 0, DateTimeKind.Unspecified);

        var passed = LocalDateTimeNormalizer.TryNormalize(input, "仅支持本地时间语义", out var normalized, out var message);

        Assert.True(passed);
        Assert.Equal(string.Empty, message);
        Assert.Equal(DateTimeKind.Local, normalized.Kind);
        Assert.Equal(input, normalized);
    }

    /// <summary>
    /// MinValue 应回退为当前本地时间。
    /// </summary>
    [Fact]
    public void TryNormalize_ShouldFallbackToNow_WhenMinValue() {
        var before = DateTime.Now;

        var passed = LocalDateTimeNormalizer.TryNormalize(DateTime.MinValue, "仅支持本地时间语义", out var normalized, out _);
        var after = DateTime.Now;

        Assert.True(passed);
        Assert.InRange(normalized, before, after);
    }
}
