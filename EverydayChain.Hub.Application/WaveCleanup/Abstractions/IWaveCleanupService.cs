namespace EverydayChain.Hub.Application.WaveCleanup.Abstractions;

/// <summary>
/// 定义波次清理服务契约。
/// </summary>
public interface IWaveCleanupService
{
    /// <summary>
    /// 按当前配置执行波次清理。
    /// </summary>
    /// <param name="waveCode">待清理的波次号。</param>
    /// <param name="ct">取消令牌。</param>
    Task<WaveCleanupResult> CleanByWaveCodeAsync(string waveCode, CancellationToken ct);

    /// <summary>
    /// 预演执行波次清理，不落任何业务变更。
    /// </summary>
    /// <param name="waveCode">待清理的波次号。</param>
    /// <param name="ct">取消令牌。</param>
    Task<WaveCleanupResult> DryRunByWaveCodeAsync(string waveCode, CancellationToken ct);

    /// <summary>
    /// 正式执行波次清理，并为本次敏感操作写入审计记录。
    /// </summary>
    /// <param name="waveCode">待清理的波次号。</param>
    /// <param name="executeContext">请求来源上下文，仅用于审计记录。</param>
    /// <param name="ct">取消令牌。</param>
    Task<WaveCleanupResult> ExecuteByWaveCodeAsync(
        string waveCode,
        EverydayChain.Hub.Application.Models.WaveCleanupExecuteContext executeContext,
        CancellationToken ct);
}
