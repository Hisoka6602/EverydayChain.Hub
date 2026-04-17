using EverydayChain.Hub.Application.Abstractions.Integrations;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.Feedback.Services;

/// <summary>
/// 业务回传应用服务，负责查询待回传任务、调用 Oracle 写入器并更新本地回传状态。
/// </summary>
public sealed class WmsFeedbackService : IWmsFeedbackService
{
    /// <summary>业务任务仓储。</summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;

    /// <summary>Oracle WMS 回传网关。</summary>
    private readonly IWmsOracleFeedbackGateway _oracleGateway;

    /// <summary>业务回传配置。</summary>
    private readonly WmsFeedbackOptions _options;

    /// <summary>日志记录器。</summary>
    private readonly ILogger<WmsFeedbackService> _logger;

    /// <summary>
    /// 初始化业务回传应用服务。
    /// </summary>
    /// <param name="businessTaskRepository">业务任务仓储。</param>
    /// <param name="oracleGateway">Oracle WMS 回传网关。</param>
    /// <param name="options">业务回传配置。</param>
    /// <param name="logger">日志记录器。</param>
    public WmsFeedbackService(
        IBusinessTaskRepository businessTaskRepository,
        IWmsOracleFeedbackGateway oracleGateway,
        WmsFeedbackOptions options,
        ILogger<WmsFeedbackService> logger)
    {
        _businessTaskRepository = businessTaskRepository;
        _oracleGateway = oracleGateway;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// 批量执行业务回传。
    /// 步骤：0. 检查回传开关；1. 查询待回传任务；2. 若无任务则返回空结果；3. 调用 Oracle 写入器；4. 按写入结果更新本地回传状态；5. 返回汇总结果。
    /// </summary>
    /// <param name="batchSize">单次处理批次大小。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>本次回传执行结果。</returns>
    public async Task<WmsFeedbackApplicationResult> ExecuteAsync(int batchSize, CancellationToken ct)
    {
        // 步骤 0：若回传开关关闭则直接返回，不消费任何待回传任务。
        if (!_options.Enabled)
        {
            _logger.LogDebug("业务回传：回传开关关闭（Enabled=false），本轮跳过执行。");
            return new WmsFeedbackApplicationResult();
        }

        // 步骤 1：查询待回传任务。
        var effectiveBatchSize = batchSize > 0 ? batchSize : 100;
        var pendingTasks = await _businessTaskRepository.FindPendingFeedbackAsync(effectiveBatchSize, ct);

        var result = new WmsFeedbackApplicationResult
        {
            PendingCount = pendingTasks.Count
        };

        // 步骤 2：若无待回传任务则直接返回。
        if (pendingTasks.Count == 0)
        {
            _logger.LogDebug("业务回传：无待回传任务，跳过本轮执行。");
            return result;
        }

        _logger.LogInformation("业务回传：本轮查询到 {Count} 个待回传任务，开始执行回传。", pendingTasks.Count);

        int successCount = 0;
        int failedCount = 0;

        // 步骤 3：整批调用 Oracle WMS 网关写入器。
        try
        {
            var writtenRows = await _oracleGateway.WriteFeedbackAsync(pendingTasks, ct);

            // 写入行数与请求任务数必须严格相等，否则视为整批失败（防止触发器/级联场景导致行数偏差引入静默错误）。
            if (writtenRows == pendingTasks.Count)
            {
                successCount = pendingTasks.Count;
                failedCount = 0;

                _logger.LogInformation(
                    "业务回传：Oracle 写入完成，整批成功。PendingCount={PendingCount}, WrittenRows={WrittenRows}",
                    pendingTasks.Count, writtenRows);
            }
            else
            {
                successCount = 0;
                failedCount = pendingTasks.Count;

                _logger.LogError(
                    "业务回传：Oracle 写入结果与待回传任务数不一致，按整批失败处理。PendingCount={PendingCount}, WrittenRows={WrittenRows}",
                    pendingTasks.Count, writtenRows);
            }
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "业务回传：Oracle 写入器执行异常，全批标记为失败。PendingCount={PendingCount}", pendingTasks.Count);
            successCount = 0;
            failedCount = pendingTasks.Count;
        }

        // 步骤 4：按写入结果更新本地回传状态。
        var now = DateTime.Now;
        if (failedCount == 0)
        {
            // 全部成功：批量更新为已回传。
            foreach (var task in pendingTasks)
            {
                task.FeedbackStatus = BusinessTaskFeedbackStatus.Completed;
                task.IsFeedbackReported = true;
                task.FeedbackTimeLocal = now;
                task.UpdatedTimeLocal = now;
                await UpdateSilentlyAsync(task, ct);
            }
        }
        else
        {
            // 整批失败：全部标为回传失败，后续由补偿重试机制处理。
            foreach (var task in pendingTasks)
            {
                task.FeedbackStatus = BusinessTaskFeedbackStatus.Failed;
                task.UpdatedTimeLocal = now;
                await UpdateSilentlyAsync(task, ct);
            }
        }

        result.SuccessCount = successCount;
        result.FailedCount = failedCount;

        _logger.LogInformation(
            "业务回传：本轮执行完毕。SuccessCount={SuccessCount}, FailedCount={FailedCount}",
            successCount, failedCount);

        return result;
    }

    /// <summary>
    /// 静默更新任务状态，取消令牌触发时向上抛出，其余异常仅记录日志不抛出。
    /// </summary>
    /// <param name="task">待更新的任务。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task UpdateSilentlyAsync(
        Domain.Aggregates.BusinessTaskAggregate.BusinessTaskEntity task,
        CancellationToken ct)
    {
        try
        {
            await _businessTaskRepository.UpdateAsync(task, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "业务回传：更新任务本地回传状态失败。TaskId={TaskId}, TaskCode={TaskCode}", task.Id, task.TaskCode);
        }
    }
}
