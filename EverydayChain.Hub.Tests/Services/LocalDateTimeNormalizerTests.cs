using EverydayChain.Hub.SharedKernel.Utilities;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 LocalDateTimeNormalizerTests 类型。
/// </summary>
public sealed class LocalDateTimeNormalizerTests {
    private const DateTimeKind NonLocalKind = (DateTimeKind)1;

    /// <summary>
    /// 执行 TryNormalize_ShouldRejectNonLocalKind 方法。
    /// </summary>
    [Fact]
    public void TryNormalize_ShouldRejectNonLocalKind() {
        // 步骤：执行 TryNormalize_ShouldRejectNonLocalKind 方法的核心处理流程。
        var input = DateTime.SpecifyKind(new DateTime(2026, 4, 13, 12, 0, 0), NonLocalKind);

        var passed = LocalDateTimeNormalizer.TryNormalize(input, "仅支持本地时间语义", out _, out var message);

        Assert.False(passed);
        Assert.Equal("仅支持本地时间语义", message);
    }

    /// <summary>
    /// 执行 TryNormalize_ShouldConvertUnspecifiedToLocal 方法。
    /// </summary>
    [Fact]
    public void TryNormalize_ShouldConvertUnspecifiedToLocal() {
        // 步骤：执行 TryNormalize_ShouldConvertUnspecifiedToLocal 方法的核心处理流程。
        var input = new DateTime(2026, 4, 13, 12, 0, 0, DateTimeKind.Unspecified);

        var passed = LocalDateTimeNormalizer.TryNormalize(input, "仅支持本地时间语义", out var normalized, out var message);

        Assert.True(passed);
        Assert.Equal(string.Empty, message);
        Assert.Equal(DateTimeKind.Local, normalized.Kind);
        Assert.Equal(input, normalized);
    }

    /// <summary>
    /// 执行 TryNormalize_ShouldFallbackToNow_WhenMinValue 方法。
    /// </summary>
    [Fact]
    public void TryNormalize_ShouldFallbackToNow_WhenMinValue() {
        // 步骤：执行 TryNormalize_ShouldFallbackToNow_WhenMinValue 方法的核心处理流程。
        var before = DateTime.Now;

        var passed = LocalDateTimeNormalizer.TryNormalize(DateTime.MinValue, "仅支持本地时间语义", out var normalized, out _);
        var after = DateTime.Now;

        Assert.True(passed);
        Assert.InRange(normalized, before, after);
    }
}

