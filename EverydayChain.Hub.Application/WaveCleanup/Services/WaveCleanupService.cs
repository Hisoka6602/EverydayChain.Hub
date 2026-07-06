using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.WaveCleanup.Abstractions;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.WaveCleanup.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class WaveCleanupService : IWaveCleanupService
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly ExceptionRuleOptions _options;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly ILogger<WaveCleanupService> _logger;

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public WaveCleanupService(
        IBusinessTaskRepository businessTaskRepository,
        ExceptionRuleOptions options,
        ILogger<WaveCleanupService> logger)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        _businessTaskRepository = businessTaskRepository;
        _options = options;
        _logger = logger;
    }

    public async Task<WaveCleanupResult> CleanByWaveCodeAsync(string waveCode, CancellationToken ct)
    {
        return await CleanByWaveCodeInternalAsync(waveCode, _options.DryRun, ct);
    }

    public async Task<WaveCleanupResult> DryRunByWaveCodeAsync(string waveCode, CancellationToken ct)
    {
        return await CleanByWaveCodeInternalAsync(waveCode, true, ct);
    }

    public async Task<WaveCleanupResult> ExecuteByWaveCodeAsync(string waveCode, CancellationToken ct)
    {
        return await CleanByWaveCodeInternalAsync(waveCode, false, ct);
    }

    private async Task<WaveCleanupResult> CleanByWaveCodeInternalAsync(string waveCode, bool dryRun, CancellationToken ct)
    {
        var safeWaveCode = SanitizeForLog(waveCode);

        if (!_options.Enabled || !_options.WaveCleanup.Enabled)
        {
            _logger.LogDebug("波次清理：规则开关关闭，跳过执行。WaveCode={WaveCode}", safeWaveCode);
            return new WaveCleanupResult
            {
                IsDryRun = dryRun,
                IdentifiedCount = 0,
                CleanedCount = 0,
                Message = "规则开关关闭，已跳过。"
            };
        }

        if (string.IsNullOrWhiteSpace(waveCode))
        {
            _logger.LogWarning("波次清理：波次编码为空，跳过执行。");
            return new WaveCleanupResult
            {
                IsDryRun = dryRun,
                IdentifiedCount = 0,
                CleanedCount = 0,
                Message = "波次编码为空，已跳过。"
            };
        }

        var trimmedWaveCode = waveCode.Trim();
        safeWaveCode = SanitizeForLog(trimmedWaveCode);

        if (!Enum.TryParse<BusinessTaskStatus>(_options.WaveCleanup.TargetStatusOnCleanup, ignoreCase: true, out var targetStatus))
        {
            _logger.LogWarning(
                "波次清理：TargetStatusOnCleanup 配置值 '{Value}' 无法解析为有效状态，回退为 Exception。",
                _options.WaveCleanup.TargetStatusOnCleanup);
            targetStatus = BusinessTaskStatus.Exception;
        }

        _logger.LogInformation(
            "波次清理：开始执行。WaveCode={WaveCode}, TargetStatus={TargetStatus}, DryRun={DryRun}",
            safeWaveCode, targetStatus, dryRun);

        var pendingTasks = await _businessTaskRepository.FindByWaveCodeAsync(trimmedWaveCode, ct);
        var nonTerminalTasks = pendingTasks
            .Where(t => t.Status != BusinessTaskStatus.Dropped && t.Status != BusinessTaskStatus.Exception)
            .ToList();

        _logger.LogInformation(
            "波次清理：波次 {WaveCode} 共查询到 {Total} 个任务，其中 {NonTerminal} 个为非终态。",
            safeWaveCode, pendingTasks.Count, nonTerminalTasks.Count);

        if (nonTerminalTasks.Count == 0)
        {
            return new WaveCleanupResult
            {
                IdentifiedCount = 0,
                CleanedCount = 0,
                IsDryRun = dryRun,
                Message = "无需清理的非终态任务。"
            };
        }

        if (dryRun)
        {
            foreach (var task in nonTerminalTasks)
            {
                _logger.LogInformation(
                    "[DryRun] 波次清理：任务 {TaskCode}（状态={Status}）将被标记为 {TargetStatus}。",
                    task.TaskCode, task.Status, targetStatus);
            }

            return new WaveCleanupResult
            {
                IdentifiedCount = nonTerminalTasks.Count,
                CleanedCount = 0,
                IsDryRun = true,
                Message = $"DryRun 模式：识别到 {nonTerminalTasks.Count} 个待清理任务，未执行实际变更。"
            };
        }

        var now = DateTime.Now;
        var failureReason = $"波次清理：波次 {safeWaveCode} 执行清理，目标状态 {targetStatus}。";
        var cleanedCount = await _businessTaskRepository.BulkMarkExceptionByWaveCodeAsync(
            trimmedWaveCode, targetStatus, failureReason, now, ct);

        _logger.LogInformation(
            "波次清理：本次执行完毕。WaveCode={WaveCode}, Identified={Identified}, Cleaned={Cleaned}",
            safeWaveCode, nonTerminalTasks.Count, cleanedCount);

        return new WaveCleanupResult
        {
            IdentifiedCount = nonTerminalTasks.Count,
            CleanedCount = cleanedCount,
            IsDryRun = false,
            Message = $"已清理 {cleanedCount}/{nonTerminalTasks.Count} 个非终态任务。"
        };
    }

    private static string SanitizeForLog(string? waveCode)
    {
        if (string.IsNullOrWhiteSpace(waveCode))
        {
            return string.Empty;
        }

        var chars = waveCode
            .Where(ch => !char.IsControl(ch))
            .ToArray();
        return new string(chars).Trim();
    }
}

