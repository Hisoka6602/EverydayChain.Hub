namespace EverydayChain.Hub.Application.WaveCleanup.Abstractions;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class WaveCleanupResult
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int IdentifiedCount { get; init; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int CleanedCount { get; init; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool IsDryRun { get; init; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? Message { get; init; }
}

