using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.WaveCleanup.Abstractions;

namespace EverydayChain.Hub.Tests.Host.Controllers;

internal sealed class StubWaveCleanupService : IWaveCleanupService
{
    /// <summary>
    /// 获取最后一次清理调用的波次号。
    /// </summary>
    public string? LastCleanWaveCode { get; private set; }

    /// <summary>
    /// 获取最后一次预演调用的波次号。
    /// </summary>
    public string? LastDryRunWaveCode { get; private set; }

    /// <summary>
    /// 获取最后一次正式执行调用的波次号。
    /// </summary>
    public string? LastExecuteWaveCode { get; private set; }

    /// <summary>
    /// 获取最后一次正式执行调用的审计上下文。
    /// </summary>
    public WaveCleanupExecuteContext? LastExecuteContext { get; private set; }

    /// <summary>
    /// 获取或设置预演结果。
    /// </summary>
    public WaveCleanupResult DryRunResult { get; set; } = new()
    {
        IdentifiedCount = 3,
        CleanedCount = 0,
        IsDryRun = true,
        Message = "DryRun 模式：识别到 3 个待清理任务，未执行实际变更。"
    };

    /// <summary>
    /// 获取或设置正式执行结果。
    /// </summary>
    public WaveCleanupResult ExecuteResult { get; set; } = new()
    {
        IdentifiedCount = 3,
        CleanedCount = 3,
        IsDryRun = false,
        Message = "已清理 3/3 个非终态任务。"
    };

    public Task<WaveCleanupResult> CleanByWaveCodeAsync(string waveCode, CancellationToken ct)
    {
        LastCleanWaveCode = waveCode;
        return Task.FromResult(ExecuteResult);
    }

    public Task<WaveCleanupResult> DryRunByWaveCodeAsync(string waveCode, CancellationToken ct)
    {
        LastDryRunWaveCode = waveCode;
        return Task.FromResult(DryRunResult);
    }

    public Task<WaveCleanupResult> ExecuteByWaveCodeAsync(
        string waveCode,
        WaveCleanupExecuteContext executeContext,
        CancellationToken ct)
    {
        LastExecuteWaveCode = waveCode;
        LastExecuteContext = executeContext;
        return Task.FromResult(ExecuteResult);
    }
}
