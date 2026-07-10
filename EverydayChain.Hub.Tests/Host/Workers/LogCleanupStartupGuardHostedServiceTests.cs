using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Host.Services;
using EverydayChain.Hub.Host.Workers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Tests.Host.Workers;

public sealed class LogCleanupStartupGuardHostedServiceTests
{
    [Fact]
    public async Task StartAsync_ShouldNotThrow_WhenGuardIsDisabled()
    {
        var hostedService = new LogCleanupStartupGuardHostedService(
            NullLogger<LogCleanupStartupGuardHostedService>.Instance,
            new LogFileMaintenanceService(
                NullLogger<LogFileMaintenanceService>.Instance,
                new StaticDiskSpaceProbe()),
            Options.Create(new LogCleanupOptions
            {
                StartupMinimumFreeSpaceMb = 0
            }));

        await hostedService.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_ShouldThrow_WhenThresholdIsNotSatisfied()
    {
        var emptyLogDirectory = Path.Combine(Path.GetTempPath(), $"hub-log-guard-empty-{Guid.NewGuid():N}");
        Directory.CreateDirectory(emptyLogDirectory);

        try
        {
            var rootPath = Path.GetPathRoot(AppContext.BaseDirectory) ?? "D:\\";
            var hostedService = new LogCleanupStartupGuardHostedService(
                NullLogger<LogCleanupStartupGuardHostedService>.Instance,
                new LogFileMaintenanceService(
                    NullLogger<LogFileMaintenanceService>.Instance,
                    new StaticDiskSpaceProbe(
                        new DiskSpaceSnapshot(rootPath, 100 * LogFileMaintenanceService.BytesPerMegabyte, 1000 * LogFileMaintenanceService.BytesPerMegabyte),
                        new DiskSpaceSnapshot(rootPath, 100 * LogFileMaintenanceService.BytesPerMegabyte, 1000 * LogFileMaintenanceService.BytesPerMegabyte))),
                Options.Create(new LogCleanupOptions
                {
                    Enabled = true,
                    LogDirectory = emptyLogDirectory,
                    RetentionDays = 30,
                    StartupMinimumFreeSpaceMb = 200
                }));

            await Assert.ThrowsAsync<InvalidOperationException>(() => hostedService.StartAsync(CancellationToken.None));
        }
        finally
        {
            Directory.Delete(emptyLogDirectory, recursive: true);
        }
    }

    private sealed class StaticDiskSpaceProbe : IDiskSpaceProbe
    {
        /// <summary>
        /// 存储 _snapshots 字段。
        /// </summary>
        private readonly Queue<DiskSpaceSnapshot> _snapshots;

        public StaticDiskSpaceProbe(params DiskSpaceSnapshot[] snapshots)
        {
            _snapshots = new Queue<DiskSpaceSnapshot>(snapshots);
            if (_snapshots.Count == 0)
            {
                _snapshots.Enqueue(new DiskSpaceSnapshot(
                    Path.GetPathRoot(AppContext.BaseDirectory) ?? "D:\\",
                    1024 * LogFileMaintenanceService.BytesPerMegabyte,
                    2048 * LogFileMaintenanceService.BytesPerMegabyte));
            }
        }

        public bool TryGetSnapshot(string path, out DiskSpaceSnapshot snapshot)
        {
            snapshot = _snapshots.Count > 1 ? _snapshots.Dequeue() : _snapshots.Peek();
            return true;
        }
    }
}
