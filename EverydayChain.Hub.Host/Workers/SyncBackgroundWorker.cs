using EverydayChain.Hub.Application.Services;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Host.Workers;

/// <summary>
/// 同步后台任务，按轮询配置触发同步编排。
/// </summary>
public class SyncBackgroundWorker(
    ISyncOrchestrator syncOrchestrator,
    IOptions<SyncJobOptions> syncJobOptions,
    ILogger<SyncBackgroundWorker> logger) : BackgroundService
{
    /// <summary>同步任务配置快照。</summary>
    private readonly SyncJobOptions _syncJobOptions = syncJobOptions.Value;

    /// <summary>
    /// 后台循环入口。
    /// </summary>
    /// <param name="stoppingToken">取消令牌。</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollingIntervalSeconds = _syncJobOptions.PollingIntervalSeconds > 0 ? _syncJobOptions.PollingIntervalSeconds : 60;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var results = await syncOrchestrator.RunAllEnabledTableSyncAsync(stoppingToken);
                foreach (var result in results)
                {
                    logger.LogInformation(
                        "同步执行完成。TableCode={TableCode}, BatchId={BatchId}, Read={ReadCount}, Insert={InsertCount}, Update={UpdateCount}, Skip={SkipCount}, ElapsedMs={ElapsedMs}",
                        result.TableCode,
                        result.BatchId,
                        result.ReadCount,
                        result.InsertCount,
                        result.UpdateCount,
                        result.SkipCount,
                        result.Elapsed.TotalMilliseconds);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "同步后台任务执行失败。");
            }

            await Task.Delay(TimeSpan.FromSeconds(pollingIntervalSeconds), stoppingToken);
        }
    }
}
