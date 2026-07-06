using System.Diagnostics;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Domain.Sync;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Host.Workers;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncBackgroundWorker(
    ISyncOrchestrator syncOrchestrator,
    IOptions<SyncJobOptions> syncJobOptions,
    ILogger<SyncBackgroundWorker> logger) : BackgroundService
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly SyncJobOptions _syncJobOptions = syncJobOptions.Value;

    /// <summary>
    /// 存储最近一次同步轮次时间戳。
    /// </summary>
    private long _lastIterationTicks = Stopwatch.GetTimestamp();

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const int WatchdogMinCheckIntervalSeconds = 30;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const int WatchdogMaxCheckIntervalSeconds = 300;

    /// <summary>
    /// 周期执行同步后台任务。
    /// </summary>
    /// <param name="stoppingToken">停止令牌。</param>
    /// <returns>后台执行任务。</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollingIntervalSeconds = _syncJobOptions.PollingIntervalSeconds > 0 ? _syncJobOptions.PollingIntervalSeconds : 60;
        var watchdogTimeoutSeconds = _syncJobOptions.WatchdogTimeoutSeconds;

        var watchdogTask = watchdogTimeoutSeconds > 0
            ? MonitorWatchdogAsync(watchdogTimeoutSeconds, pollingIntervalSeconds, stoppingToken)
            : Task.CompletedTask;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Interlocked.Exchange(ref _lastIterationTicks, Stopwatch.GetTimestamp());

                try
                {
                    await RunOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "同步后台任务执行失败。");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(pollingIntervalSeconds), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        finally
        {
            await watchdogTask;
        }
    }

    private async Task MonitorWatchdogAsync(int watchdogTimeoutSeconds, int pollingIntervalSeconds, CancellationToken ct)
    {
        var checkIntervalSeconds = Math.Clamp(watchdogTimeoutSeconds / 3, WatchdogMinCheckIntervalSeconds, WatchdogMaxCheckIntervalSeconds);
        var lastAlertTicks = 0L;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(checkIntervalSeconds), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var lastTicks = Interlocked.Read(ref _lastIterationTicks);
            var elapsedSeconds = (Stopwatch.GetTimestamp() - lastTicks) / (double)Stopwatch.Frequency;

            var threshold = (double)(watchdogTimeoutSeconds + pollingIntervalSeconds);
            if (elapsedSeconds > threshold)
            {
                var timeSinceLastAlert = lastAlertTicks == 0L
                    ? double.MaxValue
                    : (Stopwatch.GetTimestamp() - lastAlertTicks) / (double)Stopwatch.Frequency;
                if (timeSinceLastAlert >= checkIntervalSeconds)
                {
                    logger.LogCritical(
                        "同步后台任务疑似卡死，已超过看门狗超时阈值，建议立即重启服务。ElapsedSeconds={ElapsedSeconds:F0}, WatchdogThresholdSeconds={WatchdogThresholdSeconds}",
                        elapsedSeconds,
                        threshold);
                    lastAlertTicks = Stopwatch.GetTimestamp();
                }
            }
            else
            {
                lastAlertTicks = 0L;
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        var sw = Stopwatch.StartNew();
        var roundTimeoutSeconds = _syncJobOptions.TableSyncTimeoutSeconds;
        IReadOnlyList<SyncBatchResult> results;

        if (roundTimeoutSeconds > 0)
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(roundTimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeoutCts.Token);
            try
            {
                results = await syncOrchestrator.RunAllEnabledTableSyncAsync(linkedCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
            {
                logger.LogError(
                    "同步整轮执行超时，已取消本轮所有未完成表同步。TableSyncTimeoutSeconds={TableSyncTimeoutSeconds}",
                    roundTimeoutSeconds);
                return;
            }
        }
        else
        {
            results = await syncOrchestrator.RunAllEnabledTableSyncAsync(stoppingToken);
        }

        foreach (var result in results)
        {
            if (result.FailureRate > 0)
            {
                logger.LogError(
                    "同步执行失败。TableCode={TableCode}, BatchId={BatchId}, FailureRate={FailureRate:F4}, FailureMessage={FailureMessage}",
                    result.TableCode,
                    result.BatchId,
                    result.FailureRate,
                    result.FailureMessage);
                continue;
            }

            logger.LogInformation(
                "同步执行完成。TableCode={TableCode}, BatchId={BatchId}, Read={ReadCount}, Insert={InsertCount}, Update={UpdateCount}, Delete={DeleteCount}, Skip={SkipCount}, LagMinutes={LagMinutes:F2}, BacklogMinutes={BacklogMinutes:F2}, Throughput={ThroughputRowsPerSecond:F2}, ElapsedMs={ElapsedMs}",
                result.TableCode,
                result.BatchId,
                result.ReadCount,
                result.InsertCount,
                result.UpdateCount,
                result.DeleteCount,
                result.SkipCount,
                result.LagMinutes,
                result.BacklogMinutes,
                result.ThroughputRowsPerSecond,
                result.Elapsed.TotalMilliseconds);
        }

        sw.Stop();
        var totalTables = 0;
        var failedTables = 0;
        var totalRead = 0;
        var totalInsert = 0;
        var totalUpdate = 0;
        var totalDelete = 0;
        var maxLagMinutes = 0d;
        var maxBacklogMinutes = 0d;
        foreach (var r in results)
        {
            totalTables++;
            if (r.FailureRate > 0) failedTables++;
            totalRead += r.ReadCount;
            totalInsert += r.InsertCount;
            totalUpdate += r.UpdateCount;
            totalDelete += r.DeleteCount;
            if (r.LagMinutes > maxLagMinutes) maxLagMinutes = r.LagMinutes;
            if (r.BacklogMinutes > maxBacklogMinutes) maxBacklogMinutes = r.BacklogMinutes;
        }

        var successTables = totalTables - failedTables;
        var overallFailureRate = totalTables > 0 ? (double)failedTables / totalTables : 0d;
        if (failedTables > 0)
        {
            logger.LogWarning(
                "本轮同步存在失败表。TotalTables={TotalTables}, SuccessTables={SuccessTables}, FailedTables={FailedTables}, OverallFailureRate={OverallFailureRate:F4}, TotalRead={TotalRead}, TotalInsert={TotalInsert}, TotalUpdate={TotalUpdate}, TotalDelete={TotalDelete}, MaxLagMinutes={MaxLagMinutes:F2}, MaxBacklogMinutes={MaxBacklogMinutes:F2}, RoundElapsedMs={RoundElapsedMs}",
                totalTables,
                successTables,
                failedTables,
                overallFailureRate,
                totalRead,
                totalInsert,
                totalUpdate,
                totalDelete,
                maxLagMinutes,
                maxBacklogMinutes,
                sw.ElapsedMilliseconds);
        }
        else
        {
            logger.LogInformation(
                "本轮同步全部成功。TotalTables={TotalTables}, TotalRead={TotalRead}, TotalInsert={TotalInsert}, TotalUpdate={TotalUpdate}, TotalDelete={TotalDelete}, MaxLagMinutes={MaxLagMinutes:F2}, MaxBacklogMinutes={MaxBacklogMinutes:F2}, RoundElapsedMs={RoundElapsedMs}",
                totalTables,
                totalRead,
                totalInsert,
                totalUpdate,
                totalDelete,
                maxLagMinutes,
                maxBacklogMinutes,
                sw.ElapsedMilliseconds);
        }

    }
}

