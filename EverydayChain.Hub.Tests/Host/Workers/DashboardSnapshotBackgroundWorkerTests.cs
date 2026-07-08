using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Host.Workers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Tests.Host.Workers;

public sealed class DashboardSnapshotBackgroundWorkerTests
{
    [Fact]
    public async Task StartAsync_ShouldCancelSingleRun_WhenRefreshTimesOut()
    {
        var snapshotService = new BlockingDashboardSnapshotService();
        var worker = new DashboardSnapshotBackgroundWorker(
            snapshotService,
            Options.Create(new DashboardSnapshotOptions
            {
                Enabled = true,
                RefreshIntervalSeconds = 3600,
                SingleRunTimeoutSeconds = 1
            }),
            NullLogger<DashboardSnapshotBackgroundWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await snapshotService.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var cancelled = await snapshotService.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(4));

        Assert.True(cancelled);

        await worker.StopAsync(CancellationToken.None);
    }

    private sealed class BlockingDashboardSnapshotService : IDashboardSnapshotService
    {
        /// <summary>
        /// 获取刷新已启动通知。
        /// </summary>
        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// 获取刷新已取消通知。
        /// </summary>
        public TaskCompletionSource<bool> Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task RefreshAsync(CancellationToken ct)
        {
            Started.TrySetResult(true);
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            }
            catch (OperationCanceledException)
            {
                Cancelled.TrySetResult(true);
                throw;
            }
        }
    }
}
