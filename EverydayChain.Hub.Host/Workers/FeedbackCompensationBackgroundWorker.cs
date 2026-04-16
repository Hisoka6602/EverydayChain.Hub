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
                var result = await feedbackCompensationService.RetryFailedBatchAsync(batchSize, stoppingToken);
                logger.LogInformation(
                    "业务回传补偿后台任务执行完成。TargetCount={TargetCount}, RetriedCount={RetriedCount}, SuccessCount={SuccessCount}, FailedCount={FailedCount}, SkippedCount={SkippedCount}, FailureReason={FailureReason}",
                    result.TargetCount,
                    result.RetriedCount,
                    result.SuccessCount,
                    result.FailedCount,
                    result.SkippedCount,
                    result.FailureReason ?? string.Empty);
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
