using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Host.Workers;
using EverydayChain.Hub.Tests.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Tests.Host.Workers;

/// <summary>
/// 验证同步后台任务的运行期保护行为。
/// </summary>
public sealed class SyncBackgroundWorkerTests
{
    /// <summary>
    /// 验证看门狗检测到卡死时只记录严重告警。
    /// </summary>
    [Fact]
    public async Task StartAsync_ShouldLogCritical_WhenWatchdogDetectsStall()
    {
        var orchestrator = new BlockingSyncOrchestrator();
        var logger = new TestLogger<SyncBackgroundWorker>();
        var worker = new SyncBackgroundWorker(
            orchestrator,
            Options.Create(new SyncJobOptions
            {
                PollingIntervalSeconds = 1,
                TableSyncTimeoutSeconds = 0,
                WatchdogTimeoutSeconds = 1
            }),
            logger);

        await worker.StartAsync(CancellationToken.None);
        await orchestrator.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var deadline = DateTime.Now.AddSeconds(6);
        while (DateTime.Now < deadline && !HasCriticalWatchdogLog(logger))
        {
            await Task.Delay(100);
        }

        Assert.True(HasCriticalWatchdogLog(logger));
        await worker.StopAsync(CancellationToken.None);
    }

    /// <summary>
    /// 判断日志中是否已经写入看门狗严重告警。
    /// </summary>
    /// <param name="logger">测试日志记录器。</param>
    /// <returns>存在看门狗严重告警时返回 true。</returns>
    private static bool HasCriticalWatchdogLog(TestLogger<SyncBackgroundWorker> logger)
    {
        return logger.Logs.Any(log =>
            log.Level == LogLevel.Critical
            && log.Message.Contains("不主动停止宿主", StringComparison.Ordinal));
    }

    /// <summary>
    /// 模拟永不主动完成的同步编排器。
    /// </summary>
    private sealed class BlockingSyncOrchestrator : ISyncOrchestrator
    {
        /// <summary>
        /// 获取同步已经启动的通知。
        /// </summary>
        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// 执行单表同步。
        /// </summary>
        /// <param name="tableCode">表编码。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>同步结果。</returns>
        public Task<SyncBatchResult> RunTableSyncAsync(string tableCode, CancellationToken ct)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 执行所有启用表的同步并保持阻塞。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <returns>同步结果列表。</returns>
        public async Task<IReadOnlyList<SyncBatchResult>> RunAllEnabledTableSyncAsync(CancellationToken ct)
        {
            Started.TrySetResult(true);
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return [];
        }
    }
}
