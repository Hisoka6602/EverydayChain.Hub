using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Recirculation.Abstractions;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Domain.Recirculation;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.Recirculation.Services;

/// <summary>
/// 回流规则服务实现，根据任务扫描重试次数判定是否回流，并在非 dry-run 时更新任务回流状态。
/// </summary>
public sealed class RecirculationService : IRecirculationService
{
    /// <summary>业务任务仓储。</summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;

    /// <summary>异常规则配置。</summary>
    private readonly ExceptionRuleOptions _options;

    /// <summary>日志记录器。</summary>
    private readonly ILogger<RecirculationService> _logger;

    /// <summary>
    /// 初始化回流规则服务。
    /// </summary>
    /// <param name="businessTaskRepository">业务任务仓储。</param>
    /// <param name="options">异常规则配置。</param>
    /// <param name="logger">日志记录器。</param>
    public RecirculationService(
        IBusinessTaskRepository businessTaskRepository,
        ExceptionRuleOptions options,
        ILogger<RecirculationService> logger)
    {
        _businessTaskRepository = businessTaskRepository;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// 对指定业务任务执行回流判定。
    /// 步骤：0. 检查规则开关；1. 加载任务；2. 判断扫描重试次数是否超限；3. 若不超限则返回无需回流；4. dry-run 时仅记录审计日志；5. 更新任务回流状态；6. 返回结果。
    /// </summary>
    /// <param name="taskId">业务任务主键 Id。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>回流决策结果。</returns>
    public async Task<RecirculationDecisionResult> EvaluateAsync(long taskId, CancellationToken ct)
    {
        // 步骤 0：检查总开关与回流规则开关。
        if (!_options.Enabled || !_options.Recirculation.Enabled)
        {
            _logger.LogDebug("回流规则：规则开关关闭，跳过执行。TaskId={TaskId}", taskId);
            return new RecirculationDecisionResult
            {
                ShouldRecirculate = false,
                Reason = "规则开关关闭。"
            };
        }

        // 步骤 1：加载任务。
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

        // 步骤 2：获取当前扫描重试次数并与上限比较。
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

        // 步骤 3：dry-run 模式仅记录审计日志，不执行变更。
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

        // 步骤 4：更新任务回流状态。
        task.IsRecirculated = true;
        task.Status = BusinessTaskStatus.Exception;
        task.IsException = true;
        task.FailureReason = reason;
        task.UpdatedTimeLocal = DateTime.Now;
        await _businessTaskRepository.UpdateAsync(task, ct);

        _logger.LogInformation(
            "回流规则：任务 {TaskCode} 已标记为回流并设为异常状态。",
            task.TaskCode);

        // 步骤 5：返回结果。
        return new RecirculationDecisionResult
        {
            ShouldRecirculate = true,
            ScanRetryCount = retryCount,
            RecommendedStatus = BusinessTaskStatus.Exception,
            Reason = reason
        };
    }
}
