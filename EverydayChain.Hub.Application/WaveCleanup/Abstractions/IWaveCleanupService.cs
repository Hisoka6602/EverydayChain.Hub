namespace EverydayChain.Hub.Application.WaveCleanup.Abstractions;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IWaveCleanupService
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<WaveCleanupResult> CleanByWaveCodeAsync(string waveCode, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<WaveCleanupResult> DryRunByWaveCodeAsync(string waveCode, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<WaveCleanupResult> ExecuteByWaveCodeAsync(string waveCode, CancellationToken ct);
}

