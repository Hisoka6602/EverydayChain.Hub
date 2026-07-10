using EverydayChain.Hub.Application.Abstractions.Integrations;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.Feedback.Services;

/// <summary>
/// 定义 FeedbackCompensationService 类型。
/// </summary>
public sealed class FeedbackCompensationService : IFeedbackCompensationService
{
    private static readonly TimeSpan ClaimStaleAfter = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 存储 _businessTaskRepository 字段。
    /// </summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;
    /// <summary>
    /// 存储 _oracleGateway 字段。
    /// </summary>
    private readonly IWmsOracleFeedbackGateway _oracleGateway;
    /// <summary>
    /// 存储 _logger 字段。
    /// </summary>
    private readonly ILogger<FeedbackCompensationService> _logger;

    /// <summary>
    /// 执行 FeedbackCompensationService 方法。
    /// </summary>
    public FeedbackCompensationService(
        IBusinessTaskRepository businessTaskRepository,
        IWmsOracleFeedbackGateway oracleGateway,
        ILogger<FeedbackCompensationService> logger)
    {
        // 步骤：执行 FeedbackCompensationService 方法的核心处理流程。
        _businessTaskRepository = businessTaskRepository;
        _oracleGateway = oracleGateway;
        _logger = logger;
    }

    public async Task<FeedbackCompensationResult> RetryByTaskCodeAsync(string taskCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(taskCode))
        {
            return new FeedbackCompensationResult
            {
                FailureReason = "任务编码不能为空。",
                FailedCount = 1
            };
        }

        var claimedTask = await _businessTaskRepository.ClaimFeedbackByTaskCodeAsync(
            taskCode.Trim(),
            DateTime.Now,
            ClaimStaleAfter,
            ct);
        if (claimedTask is null)
        {
            _logger.LogInformation("No failed feedback task could be claimed for retry. TaskCode={TaskCode}", taskCode);
            return new FeedbackCompensationResult
            {
                TargetCount = 1,
                SkippedCount = 1
            };
        }

        return await RetryCoreAsync([claimedTask], ct);
    }

    public async Task<FeedbackCompensationResult> RetryFailedBatchAsync(int batchSize, CancellationToken ct)
    {
        var effectiveBatchSize = batchSize > 0 ? batchSize : 100;
        var claimedTasks = await _businessTaskRepository.ClaimFeedbackBatchAsync(
            BusinessTaskFeedbackStatus.Failed,
            effectiveBatchSize,
            DateTime.Now,
            ClaimStaleAfter,
            ct);
        if (claimedTasks.Count == 0)
        {
            _logger.LogDebug("No failed feedback tasks were claimed for compensation.");
            return new FeedbackCompensationResult();
        }

        return await RetryCoreAsync(claimedTasks, ct);
    }

    private async Task<FeedbackCompensationResult> RetryCoreAsync(IReadOnlyList<Domain.Aggregates.BusinessTaskAggregate.BusinessTaskEntity> tasks, CancellationToken ct)
    {
        var result = new FeedbackCompensationResult
        {
            TargetCount = tasks.Count,
            RetriedCount = tasks.Count
        };
        var taskIds = tasks.Select(task => task.Id).ToArray();
        try
        {
            var writtenRows = await _oracleGateway.WriteFeedbackAsync(tasks, ct);
            if (writtenRows == tasks.Count)
            {
                result.SuccessCount = await _businessTaskRepository.CompleteClaimedFeedbackBatchAsync(taskIds, DateTime.Now, ct);
                result.FailedCount = Math.Max(0, tasks.Count - result.SuccessCount);
                return result;
            }

            result.FailureReason = $"Oracle 回写行数不一致。已写入 {writtenRows} 行，已认领 {tasks.Count} 行。";
            result.FailedCount = await _businessTaskRepository.FailClaimedFeedbackBatchAsync(taskIds, DateTime.Now, ct);
            _logger.LogError("Feedback compensation failed because Oracle returned an unexpected row count. WrittenRows={WrittenRows}, ClaimedRows={ClaimedRows}", writtenRows, tasks.Count);
            return result;
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            result.FailureReason = "Oracle 业务回写重试失败。";
            result.FailedCount = await _businessTaskRepository.FailClaimedFeedbackBatchAsync(taskIds, DateTime.Now, ct);
            _logger.LogError(ex, "Feedback compensation execution failed after the tasks were claimed. ClaimedRows={ClaimedRows}", tasks.Count);
            return result;
        }
    }
}

