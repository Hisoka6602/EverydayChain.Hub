namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 波次清理响应。
/// </summary>
public sealed class WaveCleanupResponse {
    /// <summary>
    /// 识别到的待清理任务数。
    /// </summary>
    public int IdentifiedCount { get; set; }

    /// <summary>
    /// 实际执行清理的任务数。
    /// </summary>
    public int CleanedCount { get; set; }

    /// <summary>
    /// 是否 dry-run。
    /// </summary>
    public bool IsDryRun { get; set; }
}
