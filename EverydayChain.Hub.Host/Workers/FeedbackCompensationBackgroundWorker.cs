using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Host.Workers;

/// <summary>
/// 业务回传补偿后台任务，按配置周期重试回传失败任务。
/// </summary>
public sealed class FeedbackCompensationBackgroundWorker(
    IFeedbackCompensationService feedbackCompensationService,
    IOptions<FeedbackCompensationJobOptions> compensationJobOptions,
    ILogger<FeedbackCompensationBackgroundWorker> logger) : BackgroundService
{
    /// <summary>单轮补偿执行超时秒数（危险动作隔离器）。</summary>
    private const int SingleRunTimeoutSeconds = 300;
    /// <summary>补偿后台任务配置快照。</summary>
    private readonly FeedbackCompensationJobOptions _compensationJobOptions = compensationJobOptions.Value;

    /// <summary>
    /// 后台循环入口。
    /// </summary>
    /// <param name="stoppingToken">取消令牌。</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_compensationJobOptions.Enabled)
        {
            logger.LogInformation("业务回传补偿后台任务已禁用。");
            return;
        }

        var pollingIntervalSeconds = _compensationJobOptions.PollingIntervalSeconds > 0
            ? _compensationJobOptions.PollingIntervalSeconds
            : 300;
        var batchSize = _compensationJobOptions.BatchSize > 0
            ? _compensationJobOptions.BatchSize
            : 100;

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
