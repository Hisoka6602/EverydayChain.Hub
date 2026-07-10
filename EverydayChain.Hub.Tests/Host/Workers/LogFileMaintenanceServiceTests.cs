using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Host.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Host.Workers;

public sealed class LogFileMaintenanceServiceTests
{
    [Fact]
    public async Task CleanupOldLogsAsync_ShouldDeleteOnlyExpiredLogFiles()
    {
        var logRoot = Path.Combine(Path.GetTempPath(), $"hub-log-cleanup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(logRoot);

        try
        {
            var oldFile = Path.Combine(logRoot, "old.log");
            var newFile = Path.Combine(logRoot, "new.log");
            var ignoredFile = Path.Combine(logRoot, "keep.txt");
            await File.WriteAllTextAsync(oldFile, "old");
            await File.WriteAllTextAsync(newFile, "new");
            await File.WriteAllTextAsync(ignoredFile, "ignored");
            File.SetLastWriteTime(oldFile, DateTime.Now.AddDays(-40));
            File.SetLastWriteTime(newFile, DateTime.Now);

            var service = CreateService(new StaticDiskSpaceProbe(new DiskSpaceSnapshot(Path.GetPathRoot(logRoot) ?? "D:\\", 10 * LogFileMaintenanceService.BytesPerMegabyte, 100 * LogFileMaintenanceService.BytesPerMegabyte)));
            var summary = await service.CleanupOldLogsAsync(
                new LogCleanupOptions
                {
                    Enabled = true,
                    LogDirectory = logRoot,
                    RetentionDays = 30
                },
                CancellationToken.None);

            Assert.False(File.Exists(oldFile));
            Assert.True(File.Exists(newFile));
            Assert.True(File.Exists(ignoredFile));
            Assert.Equal(1, summary.DeletedCount);
            Assert.Equal(0, summary.TotalFailedCount);
        }
        finally
        {
            Directory.Delete(logRoot, recursive: true);
        }
    }

    [Fact]
    public async Task EnsureMinimumFreeSpaceForStartupAsync_ShouldAttemptCleanupAndRecover()
    {
        var installationDirectory = AppContext.BaseDirectory;
        var logRoot = Path.Combine(installationDirectory, "test-logs", $"guard-{Guid.NewGuid():N}");
        Directory.CreateDirectory(logRoot);

        try
        {
            var oldFile = Path.Combine(logRoot, "archive", "old.log");
            Directory.CreateDirectory(Path.GetDirectoryName(oldFile)!);
            await File.WriteAllTextAsync(oldFile, "old");
            File.SetLastWriteTime(oldFile, DateTime.Now.AddDays(-40));

            var rootPath = Path.GetPathRoot(installationDirectory) ?? "D:\\";
            var service = CreateService(new SequenceDiskSpaceProbe(
                new DiskSpaceSnapshot(rootPath, 100 * LogFileMaintenanceService.BytesPerMegabyte, 1000 * LogFileMaintenanceService.BytesPerMegabyte),
                new DiskSpaceSnapshot(rootPath, 100 * LogFileMaintenanceService.BytesPerMegabyte, 1000 * LogFileMaintenanceService.BytesPerMegabyte),
                new DiskSpaceSnapshot(rootPath, 500 * LogFileMaintenanceService.BytesPerMegabyte, 1000 * LogFileMaintenanceService.BytesPerMegabyte)));

            var result = await service.EnsureMinimumFreeSpaceForStartupAsync(
                installationDirectory,
                new LogCleanupOptions
                {
                    Enabled = true,
                    LogDirectory = logRoot,
                    RetentionDays = 30,
                    StartupMinimumFreeSpaceMb = 200
                },
                CancellationToken.None);

            Assert.True(result.IsSatisfied);
            Assert.True(result.CleanupAttempted);
            Assert.False(File.Exists(oldFile));
            Assert.NotNull(result.CleanupSummary);
            Assert.Equal(1, result.CleanupSummary!.DeletedCount);
        }
        finally
        {
            Directory.Delete(logRoot, recursive: true);
        }
    }

    private static LogFileMaintenanceService CreateService(IDiskSpaceProbe diskSpaceProbe)
    {
        return new LogFileMaintenanceService(
            NullLogger<LogFileMaintenanceService>.Instance,
            diskSpaceProbe);
    }

    private sealed class StaticDiskSpaceProbe : IDiskSpaceProbe
    {
        /// <summary>
        /// 存储 _snapshot 字段。
        /// </summary>
        private readonly DiskSpaceSnapshot _snapshot;

        public StaticDiskSpaceProbe(DiskSpaceSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public bool TryGetSnapshot(string path, out DiskSpaceSnapshot snapshot)
        {
            snapshot = _snapshot;
            return true;
        }
    }

    private sealed class SequenceDiskSpaceProbe : IDiskSpaceProbe
    {
        /// <summary>
        /// 存储 _snapshots 字段。
        /// </summary>
        private readonly Queue<DiskSpaceSnapshot> _snapshots;

        public SequenceDiskSpaceProbe(params DiskSpaceSnapshot[] snapshots)
        {
            _snapshots = new Queue<DiskSpaceSnapshot>(snapshots);
        }

        public bool TryGetSnapshot(string path, out DiskSpaceSnapshot snapshot)
        {
            if (_snapshots.Count == 0)
            {
                snapshot = default;
                return false;
            }

            snapshot = _snapshots.Dequeue();
            return true;
        }
    }
}
