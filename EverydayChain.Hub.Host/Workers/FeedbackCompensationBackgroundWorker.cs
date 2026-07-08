using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Host.Workers;

/// <summary>
/// 定义 FeedbackCompensationBackgroundWorker 类型。
/// </summary>
public sealed class FeedbackCompensationBackgroundWorker(
    IFeedbackCompensationService feedbackCompensationService,
    IOptions<FeedbackCompensationJobOptions> compensationJobOptions,
    ILogger<FeedbackCompensationBackgroundWorker> logger) : BackgroundService
{
    /// <summary>
    /// 存储 SingleRunTimeoutSeconds 字段。
    /// </summary>
    private const int SingleRunTimeoutSeconds = 300;
    /// <summary>
    /// 存储 DefaultPollingIntervalSeconds 字段。
    /// </summary>
    private const int DefaultPollingIntervalSeconds = 300;
    /// <summary>
    /// 存储 MinPollingIntervalSeconds 字段。
    /// </summary>
    private const int MinPollingIntervalSeconds = 1;
    /// <summary>
    /// 存储 MaxPollingIntervalSeconds 字段。
    /// </summary>
    private const int MaxPollingIntervalSeconds = 86400;
    /// <summary>
    /// 存储 DefaultBatchSize 字段。
    /// </summary>
    private const int DefaultBatchSize = 100;
    /// <summary>
    /// 存储 MinBatchSize 字段。
    /// </summary>
    private const int MinBatchSize = 1;
    /// <summary>
    /// 存储 MaxBatchSize 字段。
    /// </summary>
    private const int MaxBatchSize = 1000;
    /// <summary>
    /// 存储 _compensationJobOptions 字段。
    /// </summary>
    private readonly FeedbackCompensationJobOptions _compensationJobOptions = compensationJobOptions.Value;

    /// <summary>
    /// 周期执行业务回传补偿任务。
    /// </summary>
    /// <param name="stoppingToken">停止令牌。</param>
    /// <returns>后台执行任务。</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_compensationJobOptions.Enabled)
        {
            logger.LogInformation("业务回传补偿后台任务已禁用。");
            return;
        }

        var pollingCandidate = _compensationJobOptions.PollingIntervalSeconds > 0
            ? _compensationJobOptions.PollingIntervalSeconds
            : DefaultPollingIntervalSeconds;
        var batchCandidate = _compensationJobOptions.BatchSize > 0
            ? _compensationJobOptions.BatchSize
            : DefaultBatchSize;
        var pollingIntervalSeconds = Math.Clamp(pollingCandidate, MinPollingIntervalSeconds, MaxPollingIntervalSeconds);
        var batchSize = Math.Clamp(batchCandidate, MinBatchSize, MaxBatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(SingleRunTimeoutSeconds));
                var runToken = timeoutCts.Token;
                var result = await feedbackCompensationService.RetryFailedBatchAsync(batchSize, runToken);
                logger.LogInformation(
                    "业务回传补偿后台任务执行完成。TargetCount={TargetCount}, RetriedCount={RetriedCount}, SuccessCount={SuccessCount}, FailedCount={FailedCount}, SkippedCount={SkippedCount}, FailureReason={FailureReason}",
                    result.TargetCount,
                    result.RetriedCount,
                    result.SuccessCount,
                    result.FailedCount,
                    result.SkippedCount,
                    result.FailureReason ?? string.Empty);
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError("业务回传补偿后台任务单轮执行超时（>{TimeoutSeconds}s），已中断本轮并等待下个周期。", SingleRunTimeoutSeconds);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "业务回传补偿后台任务执行失败。");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(pollingIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }
}

