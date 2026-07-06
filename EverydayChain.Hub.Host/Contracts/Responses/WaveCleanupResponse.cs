namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class WaveCleanupResponse {
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int IdentifiedCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int CleanedCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool IsDryRun { get; set; }
}

