using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Host.Workers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Tests.Host.Workers;

public sealed class SyncBackgroundWorkerTests
{
    [Fact]
    public async Task StartAsync_ShouldStopHost_WhenWatchdogDetectsStall()
    {
        var hostLifetime = new RecordingHostApplicationLifetime();
        var orchestrator = new BlockingSyncOrchestrator();
        var worker = new SyncBackgroundWorker(
            orchestrator,
            Options.Create(new SyncJobOptions
            {
                PollingIntervalSeconds = 1,
                TableSyncTimeoutSeconds = 0,
                WatchdogTimeoutSeconds = 1
            }),
            hostLifetime,
            NullLogger<SyncBackgroundWorker>.Instance);

        await worker.StartAsync(hostLifetime.ApplicationStopping);
        await orchestrator.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var stopRequested = await hostLifetime.StopRequested.Task.WaitAsync(TimeSpan.FromSeconds(6));

        Assert.True(stopRequested);
        Assert.Equal(1, hostLifetime.StopCallCount);

        await worker.StopAsync(CancellationToken.None);
    }

    private sealed class BlockingSyncOrchestrator : ISyncOrchestrator
    {
        /// <summary>
        /// 获取同步已启动通知。
        /// </summary>
        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<SyncBatchResult> RunTableSyncAsync(string tableCode, CancellationToken ct)
        {
            throw new NotSupportedException();
        }

        public async Task<IReadOnlyList<SyncBatchResult>> RunAllEnabledTableSyncAsync(CancellationToken ct)
        {
            Started.TrySetResult(true);
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return [];
        }
    }

    private sealed class RecordingHostApplicationLifetime : IHostApplicationLifetime
    {
        private readonly CancellationTokenSource _applicationStopping = new();

        /// <summary>
        /// 获取应用已启动令牌。
        /// </summary>
        public CancellationToken ApplicationStarted => CancellationToken.None;

        /// <summary>
        /// 获取应用停止中令牌。
        /// </summary>
        public CancellationToken ApplicationStopping => _applicationStopping.Token;

        /// <summary>
        /// 获取应用已停止令牌。
        /// </summary>
        public CancellationToken ApplicationStopped => CancellationToken.None;

        /// <summary>
        /// 获取宿主停止调用次数。
        /// </summary>
        public int StopCallCount { get; private set; }

        /// <summary>
        /// 获取宿主停止请求通知。
        /// </summary>
        public TaskCompletionSource<bool> StopRequested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void StopApplication()
        {
            StopCallCount++;
            StopRequested.TrySetResult(true);
            _applicationStopping.Cancel();
        }
    }
}
