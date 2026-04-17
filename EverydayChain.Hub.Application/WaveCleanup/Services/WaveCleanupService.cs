using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.WaveCleanup.Abstractions;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.WaveCleanup.Services;

/// <summary>
/// 波次清理服务实现，按波次编码识别并清理所有非终态业务任务。
/// 支持 dry-run 模式，启用时仅评估并记录审计日志，不执行实际状态变更。
/// 执行清理时采用批量更新（单次数据库往返），避免 N 次上下文创建开销。
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
    /// 该入口沿用历史语义：执行模式由配置项 <c>ExceptionRule.DryRun</c> 决定。
    /// 步骤：0. 检查规则开关；1. 校验波次编码；2. 解析目标状态配置；3. 查询非终态任务数量；4. dry-run 仅记录审计日志；5. 批量更新；6. 返回结果。
    /// </summary>
    /// <param name="waveCode">波次编码，不能为空白。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>本次清理结果摘要。</returns>
    public async Task<WaveCleanupResult> CleanByWaveCodeAsync(string waveCode, CancellationToken ct)
    {
        return await CleanByWaveCodeInternalAsync(waveCode, _options.DryRun, ct);
    }

    /// <summary>
    /// 按波次编码执行 dry-run 清理评估，仅输出识别结果，不执行实际状态变更。
    /// </summary>
    /// <param name="waveCode">波次编码，不能为空白。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>本次 dry-run 评估结果。</returns>
    public async Task<WaveCleanupResult> DryRunByWaveCodeAsync(string waveCode, CancellationToken ct)
    {
        return await CleanByWaveCodeInternalAsync(waveCode, true, ct);
    }

    /// <summary>
    /// 按波次编码执行正式清理，实际推进任务状态并返回执行摘要。
    /// </summary>
    /// <param name="waveCode">波次编码，不能为空白。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>本次正式清理结果。</returns>
    public async Task<WaveCleanupResult> ExecuteByWaveCodeAsync(string waveCode, CancellationToken ct)
    {
        return await CleanByWaveCodeInternalAsync(waveCode, false, ct);
    }

    /// <summary>
    /// 按波次编码执行清理逻辑，支持按调用方指定 dry-run 或正式执行模式。
    /// </summary>
    /// <param name="waveCode">波次编码。</param>
    /// <param name="dryRun">是否 dry-run。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>清理结果。</returns>
    private async Task<WaveCleanupResult> CleanByWaveCodeInternalAsync(string waveCode, bool dryRun, CancellationToken ct)
    {
        var safeWaveCode = SanitizeForLog(waveCode);

        // 步骤 0：检查总开关与波次清理开关。
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

        // 步骤 1：校验波次编码。
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

        // 步骤 2：解析目标状态配置，无效配置时回退到 Exception 并告警。
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

        // 步骤 3：查询该波次中所有任务，过滤出非终态任务供统计与 dry-run 输出。
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

        // 步骤 4：dry-run 模式仅记录审计日志，不执行变更。
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

        // 步骤 5：批量更新非终态任务状态（单次数据库往返）。
        var now = DateTime.Now;
        var failureReason = $"波次清理：波次 {safeWaveCode} 执行清理，目标状态 {targetStatus}。";
        var cleanedCount = await _businessTaskRepository.BulkMarkExceptionByWaveCodeAsync(
            trimmedWaveCode, targetStatus, failureReason, now, ct);

        // 步骤 6：返回清理结果。
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

    /// <summary>
    /// 对日志中的波次号进行安全规范化，移除控制字符以避免日志注入。
    /// </summary>
    /// <param name="waveCode">原始波次号。</param>
    /// <returns>可安全记录到日志的波次号。</returns>
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
