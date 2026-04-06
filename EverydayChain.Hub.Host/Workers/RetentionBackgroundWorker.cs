using EverydayChain.Hub.Application.Services;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Host.Workers;

/// <summary>
/// 保留期后台任务，按配置周期触发分表保留期治理。
/// </summary>
public class RetentionBackgroundWorker(
    IRetentionExecutionService retentionExecutionService,
    IOptions<RetentionJobOptions> retentionJobOptions,
    ILogger<RetentionBackgroundWorker> logger) : BackgroundService
{
    /// <summary>保留期任务配置快照。</summary>
    private readonly RetentionJobOptions _retentionJobOptions = retentionJobOptions.Value;
    /// <summary>是否已记录过总开关关闭日志。</summary>
    private bool _dangerSwitchOffLogged;

    /// <summary>
    /// 后台循环入口。
    /// </summary>
    /// <param name="stoppingToken">取消令牌。</param>
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
                    var summary = await retentionExecutionService.ExecuteRetentionCleanupAsync(stoppingToken);
                    logger.LogInformation("保留期后台任务执行完成。Summary={Summary}", summary);
                }
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
