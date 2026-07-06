using EverydayChain.Hub.Application.Abstractions.Integrations;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.Feedback.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class WmsFeedbackService : IWmsFeedbackService
{
    private static readonly TimeSpan ClaimStaleAfter = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly IWmsOracleFeedbackGateway _oracleGateway;
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly WmsFeedbackOptions _options;
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly ILogger<WmsFeedbackService> _logger;

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public WmsFeedbackService(
        IBusinessTaskRepository businessTaskRepository,
        IWmsOracleFeedbackGateway oracleGateway,
        WmsFeedbackOptions options,
        ILogger<WmsFeedbackService> logger)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        _businessTaskRepository = businessTaskRepository;
        _oracleGateway = oracleGateway;
        _options = options;
        _logger = logger;
    }

    public async Task<WmsFeedbackApplicationResult> ExecuteAsync(int batchSize, CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Business feedback is disabled by configuration.");
            return new WmsFeedbackApplicationResult();
        }

        var effectiveBatchSize = batchSize > 0 ? batchSize : 100;
        var claimedAtLocal = DateTime.Now;
        var claimedTasks = await _businessTaskRepository.ClaimFeedbackBatchAsync(
            BusinessTaskFeedbackStatus.Pending,
            effectiveBatchSize,
            claimedAtLocal,
            ClaimStaleAfter,
            ct);
        var result = new WmsFeedbackApplicationResult
        {
            PendingCount = claimedTasks.Count
        };
        if (claimedTasks.Count == 0)
        {
            _logger.LogDebug("No pending feedback tasks were claimed in this cycle.");
            return result;
        }

        _logger.LogInformation("Claimed {Count} feedback tasks and started writeback.", claimedTasks.Count);
        var taskIds = claimedTasks.Select(task => task.Id).ToArray();
        try
        {
            var writtenRows = await _oracleGateway.WriteFeedbackAsync(claimedTasks, ct);
            if (writtenRows == claimedTasks.Count)
            {
                result.SuccessCount = await _businessTaskRepository.CompleteClaimedFeedbackBatchAsync(taskIds, DateTime.Now, ct);
                result.FailedCount = Math.Max(0, claimedTasks.Count - result.SuccessCount);
                if (result.FailedCount > 0)
                {
                    _logger.LogWarning("Only part of the claimed feedback batch was marked completed locally. Claimed={Claimed}, Completed={Completed}", claimedTasks.Count, result.SuccessCount);
                }
                else
                {
                    _logger.LogInformation("Feedback batch completed successfully. Claimed={Claimed}", claimedTasks.Count);
                }

                return result;
            }

            result.FailureReason = $"Oracle write row count mismatch. WrittenRows={writtenRows}, ClaimedRows={claimedTasks.Count}.";
            result.FailedCount = await _businessTaskRepository.FailClaimedFeedbackBatchAsync(taskIds, DateTime.Now, ct);
            _logger.LogError("Feedback batch failed because Oracle returned an unexpected row count. WrittenRows={WrittenRows}, ClaimedRows={ClaimedRows}", writtenRows, claimedTasks.Count);
            return result;
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            result.FailureReason = "Oracle feedback write failed.";
            result.FailedCount = await _businessTaskRepository.FailClaimedFeedbackBatchAsync(taskIds, DateTime.Now, ct);
            _logger.LogError(ex, "Feedback batch execution failed after the tasks were claimed. ClaimedRows={ClaimedRows}", claimedTasks.Count);
            return result;
        }
    }
}

