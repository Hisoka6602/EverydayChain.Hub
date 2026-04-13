using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.WaveCleanup.Abstractions;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.WaveCleanup.Services;

/// <summary>
/// 波次清理服务实现，按波次编码识别并清理所有非终态业务任务。
/// 支持 dry-run 模式，启用时仅评估并记录审计日志，不执行实际状态变更。
/// </summary>
public sealed class WaveCleanupService : IWaveCleanupService
{
    /// <summary>业务任务仓储。</summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;

    /// <summary>异常规则配置。</summary>
    private readonly ExceptionRuleOptions _options;

    /// <summary>日志记录器。</summary>
    private readonly ILogger<WaveCleanupService> _logger;

    /// <summary>
    /// 初始化波次清理服务。
    /// </summary>
    /// <param name="businessTaskRepository">业务任务仓储。</param>
    /// <param name="options">异常规则配置。</param>
    /// <param name="logger">日志记录器。</param>
    public WaveCleanupService(
        IBusinessTaskRepository businessTaskRepository,
        ExceptionRuleOptions options,
        ILogger<WaveCleanupService> logger)
    {
        _businessTaskRepository = businessTaskRepository;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// 按波次编码清理所有未完成（非终态）的业务任务。
    /// 步骤：0. 检查规则开关；1. 校验波次编码；2. 查询该波次未完成任务；3. 若 dry-run 仅记录审计日志后返回；4. 批量更新任务状态为异常；5. 返回清理结果。
    /// </summary>
    /// <param name="waveCode">波次编码，不能为空白。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>本次清理结果摘要。</returns>
    public async Task<WaveCleanupResult> CleanByWaveCodeAsync(string waveCode, CancellationToken ct)
    {
        // 步骤 0：检查总开关与波次清理开关。
        if (!_options.Enabled || !_options.WaveCleanup.Enabled)
        {
            _logger.LogDebug("波次清理：规则开关关闭，跳过执行。WaveCode={WaveCode}", waveCode);
            return new WaveCleanupResult { Message = "规则开关关闭，已跳过。" };
        }

        // 步骤 1：校验波次编码。
        if (string.IsNullOrWhiteSpace(waveCode))
        {
            _logger.LogWarning("波次清理：波次编码为空，跳过执行。");
            return new WaveCleanupResult { Message = "波次编码为空，已跳过。" };
        }

        var trimmedWaveCode = waveCode.Trim();
        _logger.LogInformation("波次清理：开始执行。WaveCode={WaveCode}, DryRun={DryRun}", trimmedWaveCode, _options.DryRun);

        // 步骤 2：查询该波次中所有非终态任务。
        var pendingTasks = await _businessTaskRepository.FindByWaveCodeAsync(trimmedWaveCode, ct);
        var nonTerminalTasks = pendingTasks
            .Where(t => t.Status != BusinessTaskStatus.Dropped && t.Status != BusinessTaskStatus.Exception)
            .ToList();

        _logger.LogInformation(
            "波次清理：波次 {WaveCode} 共查询到 {Total} 个任务，其中 {NonTerminal} 个为非终态。",
            trimmedWaveCode, pendingTasks.Count, nonTerminalTasks.Count);

        if (nonTerminalTasks.Count == 0)
        {
            return new WaveCleanupResult
            {
                IdentifiedCount = 0,
                CleanedCount = 0,
                IsDryRun = _options.DryRun,
                Message = "无需清理的非终态任务。"
            };
        }

        // 步骤 3：dry-run 模式仅记录审计日志，不执行变更。
        if (_options.DryRun)
        {
            foreach (var task in nonTerminalTasks)
            {
                _logger.LogInformation(
                    "[DryRun] 波次清理：任务 {TaskCode}（状态={Status}）将被标记为 {TargetStatus}。",
                    task.TaskCode, task.Status, _options.WaveCleanup.TargetStatusOnCleanup);
            }

            return new WaveCleanupResult
            {
                IdentifiedCount = nonTerminalTasks.Count,
                CleanedCount = 0,
                IsDryRun = true,
                Message = $"DryRun 模式：识别到 {nonTerminalTasks.Count} 个待清理任务，未执行实际变更。"
            };
        }

        // 步骤 4：批量更新非终态任务状态为异常。
        var now = DateTime.Now;
        int cleanedCount = 0;
        foreach (var task in nonTerminalTasks)
        {
            try
            {
                var originalStatus = task.Status;
                task.Status = BusinessTaskStatus.Exception;
                task.FailureReason = $"波次清理：波次 {trimmedWaveCode} 执行清理，原状态为 {originalStatus}。";
                task.UpdatedTimeLocal = now;
                await _businessTaskRepository.UpdateAsync(task, ct);
                cleanedCount++;
                _logger.LogInformation(
                    "波次清理：任务 {TaskCode} 已标记为异常。WaveCode={WaveCode}",
                    task.TaskCode, trimmedWaveCode);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "波次清理：任务 {TaskCode} 状态更新失败，跳过。WaveCode={WaveCode}", task.TaskCode, trimmedWaveCode);
            }
        }

        // 步骤 5：返回清理结果。
        _logger.LogInformation(
            "波次清理：本次执行完毕。WaveCode={WaveCode}, Identified={Identified}, Cleaned={Cleaned}",
            trimmedWaveCode, nonTerminalTasks.Count, cleanedCount);

        return new WaveCleanupResult
        {
            IdentifiedCount = nonTerminalTasks.Count,
            CleanedCount = cleanedCount,
            IsDryRun = false,
            Message = $"已清理 {cleanedCount}/{nonTerminalTasks.Count} 个非终态任务。"
        };
    }
}
