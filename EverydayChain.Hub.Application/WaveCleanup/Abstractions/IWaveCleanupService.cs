namespace EverydayChain.Hub.Application.WaveCleanup.Abstractions;

/// <summary>
/// 波次清理服务接口，负责按波次编码识别并清理未完成的本地业务任务。
/// </summary>
public interface IWaveCleanupService
{
    /// <summary>
    /// 按波次编码清理所有未完成（非终态）的业务任务。
    /// 若配置 dry-run 则仅评估并输出审计结论，不执行实际状态变更。
    /// </summary>
    /// <param name="waveCode">波次编码，不能为空白。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>本次清理结果摘要。</returns>
    Task<WaveCleanupResult> CleanByWaveCodeAsync(string waveCode, CancellationToken ct);
}

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
