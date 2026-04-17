using EverydayChain.Hub.Application.Abstractions.Integrations;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.Feedback.Services;

/// <summary>
/// 业务回传补偿服务，负责重试回传失败任务并回填本地回传状态。
/// </summary>
public sealed class FeedbackCompensationService : IFeedbackCompensationService
{
    /// <summary>业务任务仓储。</summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;

    /// <summary>Oracle WMS 回传网关。</summary>
    private readonly IWmsOracleFeedbackGateway _oracleGateway;

    /// <summary>日志记录器。</summary>
    private readonly ILogger<FeedbackCompensationService> _logger;

    /// <summary>
    /// 初始化补偿服务。
    /// </summary>
    /// <param name="businessTaskRepository">业务任务仓储。</param>
    /// <param name="oracleGateway">Oracle WMS 回传网关。</param>
    /// <param name="logger">日志记录器。</param>
    public FeedbackCompensationService(
        IBusinessTaskRepository businessTaskRepository,
        IWmsOracleFeedbackGateway oracleGateway,
        ILogger<FeedbackCompensationService> logger)
    {
        _businessTaskRepository = businessTaskRepository;
        _oracleGateway = oracleGateway;
        _logger = logger;
    }

    /// <summary>
    /// 按任务编码重试单条失败回传记录。
    /// 步骤：0. 校验任务编码；1. 查询任务；2. 校验回传失败状态；3. 执行单条补偿；4. 返回结果。
    /// </summary>
    /// <param name="taskCode">业务任务编码。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>补偿执行结果。</returns>
    public async Task<FeedbackCompensationResult> RetryByTaskCodeAsync(string taskCode, CancellationToken ct)
    {
        // 步骤 0：任务编码为空时直接返回失败结果。
        if (string.IsNullOrWhiteSpace(taskCode))
        {
            return new FeedbackCompensationResult
            {
                FailureReason = "任务编码不能为空。",
                FailedCount = 1
            };
        }

        // 步骤 1：按任务编码加载目标任务。
        var task = await _businessTaskRepository.FindByTaskCodeAsync(taskCode.Trim(), ct);
        if (task is null)
        {
            _logger.LogWarning("业务回传补偿：未找到任务，跳过重试。TaskCode={TaskCode}", taskCode);
            return new FeedbackCompensationResult
            {
                TargetCount = 0,
                SkippedCount = 1
            };
        }

        // 步骤 2：仅允许重试已失败任务。
        if (task.FeedbackStatus != BusinessTaskFeedbackStatus.Failed)
        {
            _logger.LogInformation(
                "业务回传补偿：任务非失败状态，跳过重试。TaskCode={TaskCode}, FeedbackStatus={FeedbackStatus}",
                task.TaskCode,
                task.FeedbackStatus);
            return new FeedbackCompensationResult
            {
                TargetCount = 1,
                SkippedCount = 1
            };
        }

        // 步骤 3：执行单条补偿。
        return await RetryCoreAsync([task], ct);
    }

    /// <summary>
    /// 按批次重试失败回传记录。
    /// 步骤：0. 归一化批次大小；1. 查询失败任务；2. 若为空返回；3. 执行批量补偿；4. 返回结果。
    /// </summary>
    /// <param name="batchSize">单次处理批次大小。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>补偿执行结果。</returns>
    public async Task<FeedbackCompensationResult> RetryFailedBatchAsync(int batchSize, CancellationToken ct)
    {
        // 步骤 0：归一化批次大小，防止非法入参导致空轮询。
        var effectiveBatchSize = batchSize > 0 ? batchSize : 100;

        // 步骤 1：查询失败任务。
        var failedTasks = await _businessTaskRepository.FindFailedFeedbackAsync(effectiveBatchSize, ct);
        if (failedTasks.Count == 0)
        {
            _logger.LogDebug("业务回传补偿：当前无失败任务，跳过本轮。");
            return new FeedbackCompensationResult();
        }

        // 步骤 2：执行批量补偿。
        return await RetryCoreAsync(failedTasks, ct);
    }

    /// <summary>
    /// 执行补偿核心逻辑：调用 Oracle 写入器并回填本地状态。
    /// </summary>
    /// <param name="tasks">待补偿任务列表。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>补偿执行结果。</returns>
    private async Task<FeedbackCompensationResult> RetryCoreAsync(IReadOnlyList<BusinessTaskEntity> tasks, CancellationToken ct)
    {
        var result = new FeedbackCompensationResult
        {
            TargetCount = tasks.Count,
            RetriedCount = tasks.Count
        };

        var successCount = 0;
        var failedCount = 0;
        var failureReason = string.Empty;

        try
        {
            var writtenRows = await _oracleGateway.WriteFeedbackAsync(tasks, ct);
            if (writtenRows == tasks.Count)
            {
                successCount = tasks.Count;
                _logger.LogInformation(
                    "业务回传补偿：批量补偿成功。TargetCount={TargetCount}, WrittenRows={WrittenRows}",
                    tasks.Count,
                    writtenRows);
            }
            else
            {
                failedCount = tasks.Count;
                failureReason = $"Oracle 写入行数与补偿任务数不一致（WrittenRows={writtenRows}, TargetCount={tasks.Count}）。";
                _logger.LogError("业务回传补偿：{FailureReason}", failureReason);
            }
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            failedCount = tasks.Count;
            failureReason = "Oracle 写入器执行异常。";
            _logger.LogError(ex, "业务回传补偿：Oracle 写入器执行异常，整批标记失败。TargetCount={TargetCount}", tasks.Count);
        }

        if (failedCount == 0)
        {
            var now = DateTime.Now;
            foreach (var task in tasks)
            {
                task.FeedbackStatus = BusinessTaskFeedbackStatus.Completed;
                task.IsFeedbackReported = true;
                task.FeedbackTimeLocal = now;
                task.UpdatedTimeLocal = now;
                await UpdateSilentlyAsync(task, ct);
            }
        }

        result.SuccessCount = successCount;
        result.FailedCount = failedCount;
        result.FailureReason = string.IsNullOrWhiteSpace(failureReason) ? null : failureReason;
        return result;
    }

    /// <summary>
    /// 静默更新任务状态，取消令牌触发时向上抛出，其余异常仅记录日志不抛出。
    /// </summary>
    /// <param name="task">待更新任务。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task UpdateSilentlyAsync(BusinessTaskEntity task, CancellationToken ct)
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
            _logger.LogError(ex, "业务回传补偿：更新任务回传状态失败。TaskId={TaskId}, TaskCode={TaskCode}", task.Id, task.TaskCode);
        }
    }
}
