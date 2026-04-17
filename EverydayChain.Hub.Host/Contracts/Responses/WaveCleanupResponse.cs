namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 波次清理响应。
/// </summary>
public sealed class WaveCleanupResponse {
    /// <summary>
    /// 识别到的待清理任务数。
    /// dry-run 与 execute 均返回该值，用于评估清理影响范围。
    /// </summary>
    public int IdentifiedCount { get; set; }

    /// <summary>
    /// 实际执行清理的任务数。
    /// dry-run 场景固定为 0；execute 场景为真实清理数量。
    /// </summary>
    public int CleanedCount { get; set; }

    /// <summary>
    /// 是否为 dry-run 执行模式。
    /// true 表示仅评估不落库删除；false 表示已执行正式清理。
    /// </summary>
    public bool IsDryRun { get; set; }
}
