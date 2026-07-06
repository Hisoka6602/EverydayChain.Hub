using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Host.Workers;

/// <summary>
/// 定义当前类型。
/// </summary>
public class RetentionBackgroundWorker(
    IRetentionExecutionService retentionExecutionService,
    IOptions<RetentionJobOptions> retentionJobOptions,
    ILogger<RetentionBackgroundWorker> logger) : BackgroundService
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const int SingleRunTimeoutSeconds = 600;
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly RetentionJobOptions _retentionJobOptions = retentionJobOptions.Value;
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private bool _dangerSwitchOffLogged;

    /// <summary>
    /// 周期执行保留期清理任务。
    /// </summary>
    /// <param name="stoppingToken">停止令牌。</param>
    /// <returns>后台执行任务。</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_retentionJobOptions.Enabled)
        {
            logger.LogInformation("保留期后台任务已禁用。");
            return;
        }

        var pollingIntervalSeconds = _retentionJobOptions.PollingIntervalSeconds > 0 ? _retentionJobOptions.PollingIntervalSeconds : 3600;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_retentionJobOptions.AllowDangerousAction)
                {
                    if (!_dangerSwitchOffLogged)
                    {
                        logger.LogWarning("保留期危险动作总开关关闭，本轮及后续周期将跳过执行。");
                        _dangerSwitchOffLogged = true;
                    }
                }
                else
                {
                    _dangerSwitchOffLogged = false;
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(SingleRunTimeoutSeconds));
                    var runToken = timeoutCts.Token;
                    var summary = await retentionExecutionService.ExecuteRetentionCleanupAsync(runToken);
                    logger.LogInformation("保留期后台任务执行完成。Summary={Summary}", summary);
                }
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError("保留期后台任务单轮执行超时（>{TimeoutSeconds}s），已中断本轮并等待下个周期。", SingleRunTimeoutSeconds);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "保留期后台任务执行失败。");
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

