using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Host.Workers;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class DashboardSnapshotBackgroundWorker(
    IDashboardSnapshotService dashboardSnapshotService,
    IOptions<DashboardSnapshotOptions> options,
    ILogger<DashboardSnapshotBackgroundWorker> logger) : BackgroundService
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly DashboardSnapshotOptions _options = options.Value;

    /// <summary>
    /// 周期刷新看板快照。
    /// </summary>
    /// <param name="stoppingToken">停止令牌。</param>
    /// <returns>后台执行任务。</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = Math.Max(5, _options.RefreshIntervalSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await dashboardSnapshotService.RefreshAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "看板快照刷新失败。");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}

