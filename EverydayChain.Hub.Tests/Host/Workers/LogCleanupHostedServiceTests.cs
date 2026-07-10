using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Host.Services;
using EverydayChain.Hub.Host.Workers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Tests.Host.Workers;

public sealed class LogCleanupHostedServiceTests
{
    [Fact]
    public async Task StartAsync_ShouldDeleteExpiredLogs_WhenEnabled()
    {
        var logRoot = Path.Combine(Path.GetTempPath(), $"hub-log-worker-{Guid.NewGuid():N}");
        Directory.CreateDirectory(logRoot);

        try
        {
            var oldFile = Path.Combine(logRoot, "old.log");
            await File.WriteAllTextAsync(oldFile, "old");
            File.SetLastWriteTime(oldFile, DateTime.Now.AddDays(-40));

            var service = new LogCleanupHostedService(
                NullLogger<LogCleanupHostedService>.Instance,
                new MutableOptionsMonitor(new LogCleanupOptions
                {
                    Enabled = true,
                    LogDirectory = logRoot,
                    RetentionDays = 30,
                    CheckIntervalHours = 24
                }),
                new LogFileMaintenanceService(
                    NullLogger<LogFileMaintenanceService>.Instance,
                    new AlwaysReadyDiskSpaceProbe(logRoot)));

            await service.StartAsync(CancellationToken.None);
            await WaitUntilAsync(() => !File.Exists(oldFile), TimeSpan.FromSeconds(2));

            Assert.False(File.Exists(oldFile));
            await service.StopAsync(CancellationToken.None);
        }
        finally
        {
            Directory.Delete(logRoot, recursive: true);
        }
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, TimeSpan timeout)
    {
        var deadline = DateTime.Now.Add(timeout);
        while (!predicate() && DateTime.Now < deadline)
        {
            await Task.Delay(50);
        }

        Assert.True(predicate());
    }

    private sealed class MutableOptionsMonitor : IOptionsMonitor<LogCleanupOptions>
    {
        /// <summary>
        /// 存储 _currentValue 字段。
        /// </summary>
        private readonly LogCleanupOptions _currentValue;

        public MutableOptionsMonitor(LogCleanupOptions currentValue)
        {
            _currentValue = currentValue;
        }

        /// <summary>
        /// 获取当前配置值。
        /// </summary>
        public LogCleanupOptions CurrentValue => _currentValue;

        public LogCleanupOptions Get(string? name)
        {
            return _currentValue;
        }

        public IDisposable? OnChange(Action<LogCleanupOptions, string?> listener)
        {
            return NullDisposable.Instance;
        }
    }

    private sealed class AlwaysReadyDiskSpaceProbe : IDiskSpaceProbe
    {
        /// <summary>
        /// 存储 _rootPath 字段。
        /// </summary>
        private readonly string _rootPath;

        public AlwaysReadyDiskSpaceProbe(string path)
        {
            _rootPath = Path.GetPathRoot(path) ?? "D:\\";
        }

        public bool TryGetSnapshot(string path, out DiskSpaceSnapshot snapshot)
        {
            snapshot = new DiskSpaceSnapshot(
                _rootPath,
                10 * LogFileMaintenanceService.BytesPerMegabyte,
                100 * LogFileMaintenanceService.BytesPerMegabyte);
            return true;
        }
    }

    private sealed class NullDisposable : IDisposable
    {
        public static NullDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
