using EverydayChain.Hub.Application.WaveCleanup.Abstractions;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 定义当前类型。
/// </summary>
internal sealed class StubWaveCleanupService : IWaveCleanupService {
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? LastCleanWaveCode { get; private set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? LastDryRunWaveCode { get; private set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? LastExecuteWaveCode { get; private set; }

    public WaveCleanupResult DryRunResult { get; set; } = new() {
        IdentifiedCount = 3,
        CleanedCount = 0,
        IsDryRun = true,
        Message = "DryRun 模式：识别到 3 个待清理任务，未执行实际变更。"
    };

    public WaveCleanupResult ExecuteResult { get; set; } = new() {
        IdentifiedCount = 3,
        CleanedCount = 3,
        IsDryRun = false,
        Message = "已清理 3/3 个非终态任务。"
    };

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public Task<WaveCleanupResult> CleanByWaveCodeAsync(string waveCode, CancellationToken ct) {
        // 步骤：按既定流程执行当前方法逻辑。
        LastCleanWaveCode = waveCode;
        return Task.FromResult(ExecuteResult);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public Task<WaveCleanupResult> DryRunByWaveCodeAsync(string waveCode, CancellationToken ct) {
        // 步骤：按既定流程执行当前方法逻辑。
        LastDryRunWaveCode = waveCode;
        return Task.FromResult(DryRunResult);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public Task<WaveCleanupResult> ExecuteByWaveCodeAsync(string waveCode, CancellationToken ct) {
        // 步骤：按既定流程执行当前方法逻辑。
        LastExecuteWaveCode = waveCode;
        return Task.FromResult(ExecuteResult);
    }
}

