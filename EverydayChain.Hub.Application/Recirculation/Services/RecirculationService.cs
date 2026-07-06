using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Recirculation.Abstractions;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Domain.Recirculation;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.Recirculation.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class RecirculationService : IRecirculationService
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
    private readonly ILogger<RecirculationService> _logger;

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public RecirculationService(
        IBusinessTaskRepository businessTaskRepository,
        ExceptionRuleOptions options,
        ILogger<RecirculationService> logger)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        _businessTaskRepository = businessTaskRepository;
        _options = options;
        _logger = logger;
    }

    public async Task<RecirculationDecisionResult> EvaluateAsync(long taskId, CancellationToken ct)
    {
        if (!_options.Enabled || !_options.Recirculation.Enabled)
        {
            _logger.LogDebug("回流规则：规则开关关闭，跳过执行。TaskId={TaskId}", taskId);
            return new RecirculationDecisionResult
            {
                ShouldRecirculate = false,
                Reason = "规则开关关闭。"
            };
        }

        var task = await _businessTaskRepository.FindByIdAsync(taskId, ct);
        if (task is null)
        {
            _logger.LogWarning("回流规则：任务不存在，跳过。TaskId={TaskId}", taskId);
            return new RecirculationDecisionResult
            {
                ShouldRecirculate = false,
                Reason = $"任务 {taskId} 不存在。"
            };
        }

        var retryCount = task.ScanRetryCount;
        var maxRetries = _options.Recirculation.MaxScanRetries;

        if (retryCount < maxRetries)
        {
            return new RecirculationDecisionResult
            {
                ShouldRecirculate = false,
                ScanRetryCount = retryCount,
                RecommendedStatus = task.Status,
                Reason = $"扫描重试次数 {retryCount} 未超过上限 {maxRetries}，无需回流。"
            };
        }

        var reason = $"扫描重试次数 {retryCount} 已达上限 {maxRetries}，触发回流。";
        _logger.LogWarning("回流规则：{Reason} TaskCode={TaskCode}", reason, task.TaskCode);

        if (_options.DryRun)
        {
            _logger.LogInformation(
                "[DryRun] 回流规则：任务 {TaskCode} 将被标记为回流（IsRecirculated=true）。",
                task.TaskCode);
            return new RecirculationDecisionResult
            {
                ShouldRecirculate = true,
                ScanRetryCount = retryCount,
                RecommendedStatus = BusinessTaskStatus.Exception,
                Reason = $"[DryRun] {reason}"
            };
        }

        task.IsRecirculated = true;
        task.Status = BusinessTaskStatus.Exception;
        task.IsException = true;
        task.FailureReason = reason;
        task.UpdatedTimeLocal = DateTime.Now;
        await _businessTaskRepository.UpdateAsync(task, ct);

        _logger.LogInformation(
            "回流规则：任务 {TaskCode} 已标记为回流并设为异常状态。",
            task.TaskCode);

        return new RecirculationDecisionResult
        {
            ShouldRecirculate = true,
            ScanRetryCount = retryCount,
            RecommendedStatus = BusinessTaskStatus.Exception,
            Reason = reason
        };
    }
}

