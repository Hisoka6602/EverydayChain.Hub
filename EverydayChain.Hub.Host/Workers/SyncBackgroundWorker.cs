using System.Diagnostics;
using EverydayChain.Hub.Application.Repositories;
using EverydayChain.Hub.Application.Services;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Domain.Sync;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Host.Workers;

/// <summary>
/// 同步后台任务，按轮询配置触发同步编排，并在每轮结束后执行空闲表内存驱逐。
/// 内置看门狗机制：当主循环超过配置阈值未推进时，输出 Critical 日志提示运维检查并重启服务。
/// </summary>
public class SyncBackgroundWorker(
    ISyncOrchestrator syncOrchestrator,
    ISyncUpsertRepository upsertRepository,
    IOptions<SyncJobOptions> syncJobOptions,
    ILogger<SyncBackgroundWorker> logger) : BackgroundService
{
    /// <summary>同步任务配置快照。</summary>
    private readonly SyncJobOptions _syncJobOptions = syncJobOptions.Value;

    /// <summary>最近一次迭代开始时的高精度时间戳（Stopwatch Ticks），供看门狗卡死检测使用。</summary>
    private long _lastIterationTicks = Stopwatch.GetTimestamp();

    /// <summary>看门狗检查间隔下限（秒）：避免检查过于频繁导致额外开销。</summary>
    private const int WatchdogMinCheckIntervalSeconds = 30;

    /// <summary>看门狗检查间隔上限（秒）：避免检查间隔过长导致响应迟滞。</summary>
    private const int WatchdogMaxCheckIntervalSeconds = 300;

    /// <summary>
    /// 后台循环入口，启动看门狗监视任务后进入主轮询循环。
    /// </summary>
    /// <param name="stoppingToken">取消令牌。</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollingIntervalSeconds = _syncJobOptions.PollingIntervalSeconds > 0 ? _syncJobOptions.PollingIntervalSeconds : 60;
        var watchdogTimeoutSeconds = _syncJobOptions.WatchdogTimeoutSeconds;

        // 若配置了看门狗超时，启动独立监视任务；否则使用已完成任务占位，避免空值判断。
        var watchdogTask = watchdogTimeoutSeconds > 0
            ? MonitorWatchdogAsync(watchdogTimeoutSeconds, pollingIntervalSeconds, stoppingToken)
            : Task.CompletedTask;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // 更新迭代心跳时间戳，看门狗依此判断主循环是否正常推进。
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

    /// <summary>
    /// 看门狗监视任务：定期检查主循环心跳，若超过阈值未推进则输出 Critical 日志（带节流，避免持续卡死时刷屏）。
    /// </summary>
    /// <param name="watchdogTimeoutSeconds">看门狗超时阈值（秒）。</param>
    /// <param name="pollingIntervalSeconds">主循环轮询间隔（秒），作为心跳缓冲窗口。</param>
    /// <param name="ct">取消令牌，服务停止时退出检测循环。</param>
    private async Task MonitorWatchdogAsync(int watchdogTimeoutSeconds, int pollingIntervalSeconds, CancellationToken ct)
    {
        // 检查间隔为超时时间的 1/3，限制在 [WatchdogMinCheckIntervalSeconds, WatchdogMaxCheckIntervalSeconds] 范围内，避免过于频繁或稀疏的检测。
        var checkIntervalSeconds = Math.Clamp(watchdogTimeoutSeconds / 3, WatchdogMinCheckIntervalSeconds, WatchdogMaxCheckIntervalSeconds);
        // 上次输出 Critical 日志时的 Stopwatch Ticks；0 表示尚未告警，心跳恢复后重置为 0。
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

            // 允许额外一个完整轮询间隔作为缓冲，避免在正常延迟期间产生误报。
            var threshold = (double)(watchdogTimeoutSeconds + pollingIntervalSeconds);
            if (elapsedSeconds > threshold)
            {
                // 节流：距离上次告警不足检查间隔时跳过，避免持续卡死期间 Critical 日志刷屏挤占磁盘/IO。
                // 首次触发时 lastAlertTicks 为 0，timeSinceLastAlert 为 MaxValue，立即告警。
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
                // 心跳恢复，重置节流状态，下次触发时立即告警。
                lastAlertTicks = 0L;
            }
        }
    }

    /// <summary>
    /// 执行单轮同步，包含各表结果汇总与整轮指标日志输出。
    /// </summary>
    /// <param name="stoppingToken">取消令牌。</param>
    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        var sw = Stopwatch.StartNew();
        var roundTimeoutSeconds = _syncJobOptions.TableSyncTimeoutSeconds;
        IReadOnlyList<SyncBatchResult> results;

        // 若配置了整轮超时，使用 CancellationTokenSource 限制本轮整体最大耗时。
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

        // 逐表输出详细结果日志。
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

        // 输出整轮汇总指标，便于在分钟级发现整体异常趋势。
        // 单次遍历同时完成计数、求和与求最大值，避免热路径对同一集合进行多次扫描。
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

        // 每轮同步结束后驱逐空闲表内存缓存，避免长期不活跃表占用内存。
        await upsertRepository.EvictIdleTablesAsync(stoppingToken);
    }
}
