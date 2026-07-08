namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示波次清理执行结果。
/// </summary>
public sealed class WaveCleanupResponse {
    /// <summary>
    /// 表示已识别出待清理任务的数量。
    /// </summary>
    public int IdentifiedCount { get; set; }

    /// <summary>
    /// 表示实际完成清理的任务数量。
    /// </summary>
    public int CleanedCount { get; set; }

    /// <summary>
    /// 表示当前结果是否来自预演执行。
    /// </summary>
    public bool IsDryRun { get; set; }
}

