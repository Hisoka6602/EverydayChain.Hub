namespace EverydayChain.Hub.Application.WaveCleanup.Abstractions;

/// <summary>
/// 波次清理执行结果。
/// </summary>
public sealed class WaveCleanupResult
{
    /// <summary>
    /// 本次识别到的待清理任务数量。
    /// </summary>
    public int IdentifiedCount { get; init; }

    /// <summary>
    /// 实际执行清理的任务数量（dry-run 时为 0）。
    /// </summary>
    public int CleanedCount { get; init; }

    /// <summary>
    /// 是否为 dry-run 模式（true 表示仅评估不执行）。
    /// </summary>
    public bool IsDryRun { get; init; }

    /// <summary>
    /// 执行说明；包含跳过原因或清理摘要。
    /// </summary>
    public string? Message { get; init; }
}
