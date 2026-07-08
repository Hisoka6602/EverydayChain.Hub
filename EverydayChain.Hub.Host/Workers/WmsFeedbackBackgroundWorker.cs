using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Host.Workers;

/// <summary>
/// 定义 WmsFeedbackBackgroundWorker 类型。
/// </summary>
public sealed class WmsFeedbackBackgroundWorker(
    IWmsFeedbackService wmsFeedbackService,
    IOptions<WmsFeedbackOptions> wmsFeedbackOptions,
    ILogger<WmsFeedbackBackgroundWorker> logger) : BackgroundService
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
    /// 存储 _wmsFeedbackOptions 字段。
    /// </summary>
    private readonly WmsFeedbackOptions _wmsFeedbackOptions = wmsFeedbackOptions.Value;

    /// <summary>
    /// 周期执行业务主回传任务。
    /// </summary>
    /// <param name="stoppingToken">停止令牌。</param>
    /// <returns>后台执行任务。</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_wmsFeedbackOptions.Enabled)
        {
            logger.LogInformation("业务回传主后台任务已禁用。");
            return;
        }

        var pollingCandidate = _wmsFeedbackOptions.PollingIntervalSeconds > 0
            ? _wmsFeedbackOptions.PollingIntervalSeconds
            : DefaultPollingIntervalSeconds;
        var batchCandidate = _wmsFeedbackOptions.BatchSize > 0
            ? _wmsFeedbackOptions.BatchSize
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
                var result = await wmsFeedbackService.ExecuteAsync(batchSize, runToken);
                var isSkipped = result.PendingCount == 0;

                logger.LogInformation(
                    "业务回传主后台任务执行完成。PendingCount={PendingCount}, SuccessCount={SuccessCount}, FailedCount={FailedCount}, IsSkipped={IsSkipped}, FailureReason={FailureReason}",
                    result.PendingCount,
                    result.SuccessCount,
                    result.FailedCount,
                    isSkipped,
                    result.FailureReason ?? string.Empty);
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError("业务回传主后台任务单轮执行超时（>{TimeoutSeconds}s），已中断本轮并等待下个周期。", SingleRunTimeoutSeconds);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "业务回传主后台任务执行失败。");
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

