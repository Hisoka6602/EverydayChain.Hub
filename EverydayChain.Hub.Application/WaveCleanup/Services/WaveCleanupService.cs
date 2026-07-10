using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.WaveCleanup.Abstractions;
using EverydayChain.Hub.Domain.Aggregates.AuditLogs;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.WaveCleanup.Services;

/// <summary>
/// 提供波次清理执行能力。
/// 该服务只会批量改写业务任务状态，不会物理删除业务任务表、扫描日志表或落格日志表中的历史数据。
/// </summary>
public sealed class WaveCleanupService : IWaveCleanupService
{
    /// <summary>
    /// 存储业务任务仓储。
    /// </summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;

    /// <summary>
    /// 存储波次清理审计仓储。
    /// </summary>
    private readonly IWaveCleanupAuditLogRepository _waveCleanupAuditLogRepository;

    /// <summary>
    /// 存储异常规则配置。
    /// </summary>
    private readonly ExceptionRuleOptions _options;

    /// <summary>
    /// 存储日志记录器。
    /// </summary>
    private readonly ILogger<WaveCleanupService> _logger;

    /// <summary>
    /// 初始化波次清理服务。
    /// </summary>
    /// <param name="businessTaskRepository">业务任务仓储。</param>
    /// <param name="waveCleanupAuditLogRepository">波次清理审计仓储。</param>
    /// <param name="options">异常规则配置。</param>
    /// <param name="logger">日志记录器。</param>
    public WaveCleanupService(
        IBusinessTaskRepository businessTaskRepository,
        IWaveCleanupAuditLogRepository waveCleanupAuditLogRepository,
        ExceptionRuleOptions options,
        ILogger<WaveCleanupService> logger)
    {
        // 步骤：保存依赖，供后续波次查询、状态批量更新和敏感操作审计使用。
        _businessTaskRepository = businessTaskRepository;
        _waveCleanupAuditLogRepository = waveCleanupAuditLogRepository;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// 按当前配置执行波次清理。
    /// </summary>
    /// <param name="waveCode">波次号。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>执行结果。</returns>
    public async Task<WaveCleanupResult> CleanByWaveCodeAsync(string waveCode, CancellationToken ct)
    {
        return await CleanByWaveCodeInternalAsync(waveCode, _options.DryRun, ct);
    }

    /// <summary>
    /// 预演执行波次清理。
    /// </summary>
    /// <param name="waveCode">波次号。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>预演结果。</returns>
    public async Task<WaveCleanupResult> DryRunByWaveCodeAsync(string waveCode, CancellationToken ct)
    {
        return await CleanByWaveCodeInternalAsync(waveCode, true, ct);
    }

    /// <summary>
    /// 正式执行波次清理，并为敏感操作写入审计记录。
    /// </summary>
    /// <param name="waveCode">波次号。</param>
    /// <param name="executeContext">请求来源上下文。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>正式执行结果。</returns>
    public async Task<WaveCleanupResult> ExecuteByWaveCodeAsync(
        string waveCode,
        WaveCleanupExecuteContext executeContext,
        CancellationToken ct)
    {
        // 步骤：先写开始审计，再执行状态改写，最后回写结果，保证敏感操作全程可追踪。
        var requestedTimeLocal = DateTime.Now;
        var targetStatus = ResolveTargetStatus();
        var auditLog = BuildAuditLog(waveCode, targetStatus, requestedTimeLocal, executeContext);
        await _waveCleanupAuditLogRepository.SaveAsync(auditLog, ct);

        try
        {
            var result = await CleanByWaveCodeInternalAsync(waveCode, dryRun: false, ct);
            await _waveCleanupAuditLogRepository.UpdateResultAsync(
                auditLog.Id,
                executionStage: "Completed",
                identifiedCount: result.IdentifiedCount,
                cleanedCount: result.CleanedCount,
                message: result.Message ?? string.Empty,
                completedTimeLocal: DateTime.Now,
                ct);
            return result;
        }
        catch (Exception ex)
        {
            var safeWaveCode = SanitizeForLog(waveCode);
            _logger.LogError(ex, "波次清理：正式执行发生异常。WaveCode={WaveCode}", safeWaveCode);
            await _waveCleanupAuditLogRepository.UpdateResultAsync(
                auditLog.Id,
                executionStage: "Failed",
                identifiedCount: 0,
                cleanedCount: 0,
                message: $"执行异常：{TrimToLength(SanitizeForLog(ex.Message), 480)}",
                completedTimeLocal: DateTime.Now,
                ct);
            throw;
        }
    }

    /// <summary>
    /// 执行波次清理核心逻辑。
    /// </summary>
    /// <param name="waveCode">波次号。</param>
    /// <param name="dryRun">是否预演。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>执行结果。</returns>
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
        var targetStatus = ResolveTargetStatus();

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
                    "预演模式：波次清理任务 {TaskCode}（状态={Status}）将被标记为 {TargetStatus}。",
                    task.TaskCode, task.Status, targetStatus);
            }

            return new WaveCleanupResult
            {
                IdentifiedCount = nonTerminalTasks.Count,
                CleanedCount = 0,
                IsDryRun = true,
                Message = $"预演模式：识别到 {nonTerminalTasks.Count} 个待清理任务，未执行实际变更。"
            };
        }

        var now = DateTime.Now;
        var failureReason = $"波次清理：波次 {safeWaveCode} 执行清理，目标状态 {targetStatus}。";
        var cleanedCount = await _businessTaskRepository.BulkMarkExceptionByWaveCodeAsync(
            trimmedWaveCode,
            targetStatus,
            failureReason,
            now,
            ct);

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
    /// 解析波次清理目标状态。
    /// </summary>
    /// <returns>合法的目标状态。</returns>
    private BusinessTaskStatus ResolveTargetStatus()
    {
        // 步骤：优先读取配置；如果配置非法，则稳定回退到 Exception。
        if (!Enum.TryParse<BusinessTaskStatus>(_options.WaveCleanup.TargetStatusOnCleanup, ignoreCase: true, out var targetStatus))
        {
            _logger.LogWarning(
                "波次清理：TargetStatusOnCleanup 配置值 '{Value}' 无法解析为有效状态，回退为 Exception。",
                _options.WaveCleanup.TargetStatusOnCleanup);
            targetStatus = BusinessTaskStatus.Exception;
        }

        return targetStatus;
    }

    /// <summary>
    /// 构建波次清理开始审计记录。
    /// </summary>
    /// <param name="waveCode">波次号。</param>
    /// <param name="targetStatus">目标状态。</param>
    /// <param name="requestedTimeLocal">请求时间。</param>
    /// <param name="executeContext">请求上下文。</param>
    /// <returns>审计实体。</returns>
    private WaveCleanupAuditLogEntity BuildAuditLog(
        string waveCode,
        BusinessTaskStatus targetStatus,
        DateTime requestedTimeLocal,
        WaveCleanupExecuteContext executeContext)
    {
        // 步骤：统一清洗输入，避免敏感操作审计中混入控制字符或超长文本。
        return new WaveCleanupAuditLogEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            WaveCode = TrimToLength(SanitizeForLog(waveCode), 64),
            TargetStatus = TrimToLength(targetStatus.ToString(), 32),
            ExecutionStage = "Started",
            IdentifiedCount = 0,
            CleanedCount = 0,
            Message = "已接收正式波次清理请求，等待执行结果。",
            RequestedTimeLocal = requestedTimeLocal,
            CompletedTimeLocal = null,
            TraceId = TrimToLength(SanitizeForLog(executeContext.TraceId), 128),
            RequestPath = TrimToLength(SanitizeForLog(executeContext.RequestPath), 128),
            HttpMethod = TrimToLength(SanitizeForLog(executeContext.HttpMethod), 16),
            OperatorId = TrimToLength(SanitizeForLog(executeContext.OperatorId), 64),
            ClientIp = TrimToLength(SanitizeForLog(executeContext.ClientIp), 64),
            UserAgent = TrimToLength(SanitizeForLog(executeContext.UserAgent), 256)
        };
    }

    /// <summary>
    /// 按最大长度裁剪文本。
    /// </summary>
    /// <param name="value">原始文本。</param>
    /// <param name="maxLength">允许的最大长度。</param>
    /// <returns>裁剪后的文本。</returns>
    private static string TrimToLength(string? value, int maxLength)
    {
        // 步骤：空值回退为空字符串；长度超限时仅截断，不改变原有业务语义。
        var normalizedValue = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        if (normalizedValue.Length <= maxLength)
        {
            return normalizedValue;
        }

        return normalizedValue[..maxLength];
    }

    /// <summary>
    /// 清洗日志和审计文本，去除控制字符。
    /// </summary>
    /// <param name="value">原始文本。</param>
    /// <returns>清洗后的文本。</returns>
    private static string SanitizeForLog(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var chars = value
            .Where(ch => !char.IsControl(ch))
            .ToArray();
        return new string(chars).Trim();
    }
}
