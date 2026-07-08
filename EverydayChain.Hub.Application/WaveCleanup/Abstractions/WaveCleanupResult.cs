namespace EverydayChain.Hub.Application.WaveCleanup.Abstractions;

/// <summary>
/// 定义 WaveCleanupResult 类型。
/// </summary>
public sealed class WaveCleanupResult
{
    /// <summary>
    /// 获取或设置 IdentifiedCount。
    /// </summary>
    public int IdentifiedCount { get; init; }

    /// <summary>
    /// 获取或设置 CleanedCount。
    /// </summary>
    public int CleanedCount { get; init; }

    /// <summary>
    /// 获取或设置 IsDryRun。
    /// </summary>
    public bool IsDryRun { get; init; }

    /// <summary>
    /// 获取或设置 Message。
    /// </summary>
    public string? Message { get; init; }
}

