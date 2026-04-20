using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Host.Workers;

/// <summary>
/// 业务回传主后台任务，按配置周期消费待回传任务并触发 Oracle 回写。
/// </summary>
public sealed class WmsFeedbackBackgroundWorker(
    IWmsFeedbackService wmsFeedbackService,
    IOptions<WmsFeedbackOptions> wmsFeedbackOptions,
    ILogger<WmsFeedbackBackgroundWorker> logger) : BackgroundService
{
    /// <summary>单轮主回传执行超时秒数（危险动作隔离器）。</summary>
    private const int SingleRunTimeoutSeconds = 300;

    /// <summary>业务回传配置快照。</summary>
    private readonly WmsFeedbackOptions _wmsFeedbackOptions = wmsFeedbackOptions.Value;

    /// <summary>
    /// 后台循环入口。
    /// </summary>
    /// <param name="stoppingToken">取消令牌。</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_wmsFeedbackOptions.Enabled)
        {
            logger.LogInformation("业务回传主后台任务已禁用。");
            return;
        }

        var pollingIntervalSeconds = _wmsFeedbackOptions.PollingIntervalSeconds > 0
            ? _wmsFeedbackOptions.PollingIntervalSeconds
            : 300;
        var batchSize = _wmsFeedbackOptions.BatchSize > 0
            ? _wmsFeedbackOptions.BatchSize
            : 100;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(SingleRunTimeoutSeconds));
                var runToken = timeoutCts.Token;
                var result = await wmsFeedbackService.ExecuteAsync(batchSize, runToken);
                var skipped = result.PendingCount == 0;

                logger.LogInformation(
                    "业务回传主后台任务执行完成。PendingCount={PendingCount}, SuccessCount={SuccessCount}, FailedCount={FailedCount}, Skipped={Skipped}",
                    result.PendingCount,
                    result.SuccessCount,
                    result.FailedCount,
                    skipped);
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
