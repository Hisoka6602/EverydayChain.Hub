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
