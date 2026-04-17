using EverydayChain.Hub.Application.WaveCleanup.Abstractions;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 波次清理服务桩实现。
/// </summary>
internal sealed class StubWaveCleanupService : IWaveCleanupService {
    /// <summary>
    /// 最近一次兼容入口清理入参。
    /// </summary>
    public string? LastCleanWaveCode { get; private set; }

    /// <summary>
    /// 最近一次 dry-run 入参。
    /// </summary>
    public string? LastDryRunWaveCode { get; private set; }

    /// <summary>
    /// 最近一次正式执行入参。
    /// </summary>
    public string? LastExecuteWaveCode { get; private set; }

    /// <summary>
    /// dry-run 结果。
    /// </summary>
    public WaveCleanupResult DryRunResult { get; set; } = new() {
        IdentifiedCount = 3,
        CleanedCount = 0,
        IsDryRun = true,
        Message = "DryRun 模式：识别到 3 个待清理任务，未执行实际变更。"
    };

    /// <summary>
    /// 正式执行结果。
    /// </summary>
    public WaveCleanupResult ExecuteResult { get; set; } = new() {
        IdentifiedCount = 3,
        CleanedCount = 3,
        IsDryRun = false,
        Message = "已清理 3/3 个非终态任务。"
    };

    /// <inheritdoc/>
    public Task<WaveCleanupResult> CleanByWaveCodeAsync(string waveCode, CancellationToken ct) {
        LastCleanWaveCode = waveCode;
        return Task.FromResult(ExecuteResult);
    }

    /// <inheritdoc/>
    public Task<WaveCleanupResult> DryRunByWaveCodeAsync(string waveCode, CancellationToken ct) {
        LastDryRunWaveCode = waveCode;
        return Task.FromResult(DryRunResult);
    }

    /// <inheritdoc/>
    public Task<WaveCleanupResult> ExecuteByWaveCodeAsync(string waveCode, CancellationToken ct) {
        LastExecuteWaveCode = waveCode;
        return Task.FromResult(ExecuteResult);
    }
}
