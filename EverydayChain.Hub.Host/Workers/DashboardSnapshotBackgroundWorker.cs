using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Host.Workers;

/// <summary>
/// 定义 DashboardSnapshotBackgroundWorker 类型。
/// </summary>
public sealed class DashboardSnapshotBackgroundWorker(
    IDashboardSnapshotService dashboardSnapshotService,
    IOptions<DashboardSnapshotOptions> options,
    ILogger<DashboardSnapshotBackgroundWorker> logger) : BackgroundService
{
    /// <summary>
    /// 存储 DefaultRefreshIntervalSeconds 字段。
    /// </summary>
    private const int DefaultRefreshIntervalSeconds = 15;

    /// <summary>
    /// 存储 MinRefreshIntervalSeconds 字段。
    /// </summary>
    private const int MinRefreshIntervalSeconds = 5;

    /// <summary>
    /// 存储 MaxRefreshIntervalSeconds 字段。
    /// </summary>
    private const int MaxRefreshIntervalSeconds = 3600;

    /// <summary>
    /// 存储 DefaultSingleRunTimeoutSeconds 字段。
    /// </summary>
    private const int DefaultSingleRunTimeoutSeconds = 300;

    /// <summary>
    /// 存储 MinSingleRunTimeoutSeconds 字段。
    /// </summary>
    private const int MinSingleRunTimeoutSeconds = 1;

    /// <summary>
    /// 存储 MaxSingleRunTimeoutSeconds 字段。
    /// </summary>
    private const int MaxSingleRunTimeoutSeconds = 3600;

    /// <summary>
    /// 存储 _options 字段。
    /// </summary>
    private readonly DashboardSnapshotOptions _options = options.Value;

    /// <summary>
    /// 周期刷新看板快照。
    /// </summary>
    /// <param name="stoppingToken">停止令牌。</param>
    /// <returns>后台执行任务。</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("看板快照后台任务已禁用。");
            return;
        }

        var intervalCandidate = _options.RefreshIntervalSeconds > 0
            ? _options.RefreshIntervalSeconds
            : DefaultRefreshIntervalSeconds;
        var timeoutCandidate = _options.SingleRunTimeoutSeconds > 0
            ? _options.SingleRunTimeoutSeconds
            : DefaultSingleRunTimeoutSeconds;
        var intervalSeconds = Math.Clamp(intervalCandidate, MinRefreshIntervalSeconds, MaxRefreshIntervalSeconds);
        var timeoutSeconds = Math.Clamp(timeoutCandidate, MinSingleRunTimeoutSeconds, MaxSingleRunTimeoutSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
                await dashboardSnapshotService.RefreshAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError("看板快照后台任务单轮执行超时（>{TimeoutSeconds}s），已中断本轮并等待下个周期。", timeoutSeconds);
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

