using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Host.Services;

/// <summary>
/// 提供日志文件清理与启动磁盘容量检查能力。
/// </summary>
public sealed class LogFileMaintenanceService
{
    /// <summary>
    /// 存储 BytesPerMegabyte 常量。
    /// </summary>
    public const long BytesPerMegabyte = 1024L * 1024L;

    /// <summary>
    /// 存储 _logger 字段。
    /// </summary>
    private readonly ILogger<LogFileMaintenanceService> _logger;

    /// <summary>
    /// 存储 _diskSpaceProbe 字段。
    /// </summary>
    private readonly IDiskSpaceProbe _diskSpaceProbe;

    /// <summary>
    /// 初始化日志文件维护服务。
    /// </summary>
    /// <param name="logger">日志记录器。</param>
    /// <param name="diskSpaceProbe">磁盘空间探测器。</param>
    public LogFileMaintenanceService(
        ILogger<LogFileMaintenanceService> logger,
        IDiskSpaceProbe diskSpaceProbe)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _diskSpaceProbe = diskSpaceProbe ?? throw new ArgumentNullException(nameof(diskSpaceProbe));
    }

    /// <summary>
    /// 解析日志目录绝对路径。
    /// </summary>
    /// <param name="configuredLogDirectory">配置中的日志目录。</param>
    /// <returns>绝对路径。</returns>
    public string ResolveLogDirectory(string configuredLogDirectory)
    {
        if (string.IsNullOrWhiteSpace(configuredLogDirectory))
        {
            throw new ArgumentException("日志目录不能为空。", nameof(configuredLogDirectory));
        }

        return Path.IsPathRooted(configuredLogDirectory)
            ? configuredLogDirectory
            : Path.Combine(AppContext.BaseDirectory, configuredLogDirectory);
    }

    /// <summary>
    /// 清理超过保留期的日志文件。
    /// </summary>
    /// <param name="options">日志清理配置。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>清理摘要。</returns>
    public Task<LogCleanupSummary> CleanupOldLogsAsync(LogCleanupOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        var logDirectory = ResolveLogDirectory(options.LogDirectory);
        if (!Directory.Exists(logDirectory))
        {
            _logger.LogWarning("日志目录不存在，已跳过清理。LogDirectory={LogDirectory}", logDirectory);
            return Task.FromResult(new LogCleanupSummary(logDirectory, 0, 0, 0));
        }

        LogCapacityWarnings(logDirectory, options);

        var cutoffDate = DateTime.Now.AddDays(-options.RetentionDays);
        _logger.LogInformation(
            "开始清理过期日志。LogDirectory={LogDirectory}, CutoffDate={CutoffDate:yyyy-MM-dd HH:mm:ss}",
            logDirectory,
            cutoffDate);

        var summary = CleanupDirectory(logDirectory, cutoffDate, cancellationToken);
        _logger.LogInformation(
            "日志清理完成。LogDirectory={LogDirectory}, DeletedCount={DeletedCount}, DeleteFailedCount={DeleteFailedCount}, ScanFailedCount={ScanFailedCount}, TotalFailedCount={TotalFailedCount}",
            summary.LogDirectory,
            summary.DeletedCount,
            summary.FailedCount,
            summary.ScanFailedCount,
            summary.TotalFailedCount);

        return Task.FromResult(summary);
    }

    /// <summary>
    /// 启动前确保所在磁盘剩余空间达到阈值。
    /// </summary>
    /// <param name="installationDirectory">宿主安装目录。</param>
    /// <param name="options">日志清理配置。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>检查结果。</returns>
    public async Task<StartupDiskSpaceGuardResult> EnsureMinimumFreeSpaceForStartupAsync(
        string installationDirectory,
        LogCleanupOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installationDirectory);
        ArgumentNullException.ThrowIfNull(options);

        if (options.StartupMinimumFreeSpaceMb <= 0)
        {
            return StartupDiskSpaceGuardResult.Disabled;
        }

        if (!_diskSpaceProbe.TryGetSnapshot(installationDirectory, out var beforeSnapshot))
        {
            return StartupDiskSpaceGuardResult.InspectionFailed(
                $"无法评估宿主安装目录所在磁盘剩余空间。InstallationDirectory={installationDirectory}");
        }

        if (beforeSnapshot.AvailableFreeSpaceMb >= options.StartupMinimumFreeSpaceMb)
        {
            _logger.LogInformation(
                "启动磁盘门禁检查通过。RootPath={RootPath}, FreeMb={FreeMb:F2}, ThresholdMb={ThresholdMb}",
                beforeSnapshot.RootPath,
                beforeSnapshot.AvailableFreeSpaceMb,
                options.StartupMinimumFreeSpaceMb);
            return StartupDiskSpaceGuardResult.Passed(beforeSnapshot);
        }

        var logDirectory = ResolveLogDirectory(options.LogDirectory);
        var installationRoot = Path.GetPathRoot(Path.GetFullPath(installationDirectory)) ?? string.Empty;
        var logRoot = Path.GetPathRoot(Path.GetFullPath(logDirectory)) ?? string.Empty;

        _logger.LogWarning(
            "启动磁盘剩余空间不足，准备尝试清理日志。InstallationRoot={InstallationRoot}, FreeMb={FreeMb:F2}, ThresholdMb={ThresholdMb}, LogDirectory={LogDirectory}",
            installationRoot,
            beforeSnapshot.AvailableFreeSpaceMb,
            options.StartupMinimumFreeSpaceMb,
            logDirectory);

        if (!string.Equals(installationRoot, logRoot, StringComparison.OrdinalIgnoreCase))
        {
            return StartupDiskSpaceGuardResult.Failed(
                beforeSnapshot,
                null,
                null,
                $"安装目录磁盘与日志目录磁盘不同，清理日志无法提升启动盘空间。InstallationRoot={installationRoot}, LogRoot={logRoot}");
        }

        var cleanupSummary = await CleanupOldLogsAsync(options, cancellationToken);
        if (!_diskSpaceProbe.TryGetSnapshot(installationDirectory, out var afterSnapshot))
        {
            return StartupDiskSpaceGuardResult.Failed(
                beforeSnapshot,
                null,
                cleanupSummary,
                $"日志清理完成后仍无法重新评估启动盘剩余空间。InstallationDirectory={installationDirectory}");
        }

        if (afterSnapshot.AvailableFreeSpaceMb >= options.StartupMinimumFreeSpaceMb)
        {
            _logger.LogInformation(
                "启动磁盘门禁恢复成功。RootPath={RootPath}, FreeMbBefore={FreeMbBefore:F2}, FreeMbAfter={FreeMbAfter:F2}, ThresholdMb={ThresholdMb}",
                afterSnapshot.RootPath,
                beforeSnapshot.AvailableFreeSpaceMb,
                afterSnapshot.AvailableFreeSpaceMb,
                options.StartupMinimumFreeSpaceMb);
            return StartupDiskSpaceGuardResult.Recovered(beforeSnapshot, afterSnapshot, cleanupSummary);
        }

        return StartupDiskSpaceGuardResult.Failed(
            beforeSnapshot,
            afterSnapshot,
            cleanupSummary,
            $"日志清理后启动盘剩余空间仍不足。RootPath={afterSnapshot.RootPath}, FreeMb={afterSnapshot.AvailableFreeSpaceMb:F2}, ThresholdMb={options.StartupMinimumFreeSpaceMb}");
    }

    private LogCleanupSummary CleanupDirectory(string directory, DateTime cutoffDate, CancellationToken cancellationToken)
    {
        var deletedCount = 0;
        var failedCount = 0;
        var scanFailedCount = 0;
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(directory);

        while (pendingDirectories.Count > 0)
        {
            var currentDirectory = pendingDirectories.Pop();
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("日志清理收到取消信号，停止扫描。Directory={Directory}", currentDirectory);
                return new LogCleanupSummary(directory, deletedCount, failedCount, scanFailedCount);
            }

            try
            {
                foreach (var filePath in Directory.EnumerateFiles(currentDirectory, "*.log", SearchOption.TopDirectoryOnly))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("日志清理收到取消信号，停止扫描。Directory={Directory}", currentDirectory);
                        return new LogCleanupSummary(directory, deletedCount, failedCount, scanFailedCount);
                    }

                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        if (fileInfo.LastWriteTime < cutoffDate)
                        {
                            fileInfo.Delete();
                            deletedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "删除日志文件失败。FilePath={FilePath}", filePath);
                        failedCount++;
                    }
                }

                foreach (var subDirectory in Directory.EnumerateDirectories(currentDirectory, "*", SearchOption.TopDirectoryOnly))
                {
                    pendingDirectories.Push(subDirectory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "扫描日志目录失败。Directory={Directory}", currentDirectory);
                scanFailedCount++;
            }
        }

        return new LogCleanupSummary(directory, deletedCount, failedCount, scanFailedCount);
    }

    private void LogCapacityWarnings(string logDirectory, LogCleanupOptions options)
    {
        try
        {
            var directorySizeBytes = GetDirectorySizeBytes(logDirectory);
            if (options.LogDirectorySizeMbWarningThreshold > 0)
            {
                var thresholdBytes = options.LogDirectorySizeMbWarningThreshold * BytesPerMegabyte;
                if (directorySizeBytes >= thresholdBytes)
                {
                    _logger.LogWarning(
                        "日志目录容量超过告警阈值。LogDirectory={LogDirectory}, SizeMb={SizeMb:F2}, ThresholdMb={ThresholdMb}",
                        logDirectory,
                        decimal.Round(directorySizeBytes / (decimal)BytesPerMegabyte, 3),
                        options.LogDirectorySizeMbWarningThreshold);
                }
            }

            if (!_diskSpaceProbe.TryGetSnapshot(logDirectory, out var snapshot))
            {
                _logger.LogWarning("日志目录所在磁盘不可用，无法输出磁盘容量告警。LogDirectory={LogDirectory}", logDirectory);
                return;
            }

            if (options.LowDiskFreeSpaceMbWarningThreshold > 0
                && snapshot.AvailableFreeSpaceMb <= options.LowDiskFreeSpaceMbWarningThreshold)
            {
                _logger.LogWarning(
                    "日志目录所在磁盘剩余容量低于阈值。RootPath={RootPath}, FreeMb={FreeMb:F2}, ThresholdMb={ThresholdMb}",
                    snapshot.RootPath,
                    snapshot.AvailableFreeSpaceMb,
                    options.LowDiskFreeSpaceMbWarningThreshold);
            }

            if (options.LowDiskFreeSpacePercentWarningThreshold > 0 && snapshot.TotalSizeBytes > 0)
            {
                var freePercent = decimal.Round(
                    snapshot.AvailableFreeSpaceBytes * 100m / snapshot.TotalSizeBytes,
                    3);
                if (freePercent <= options.LowDiskFreeSpacePercentWarningThreshold)
                {
                    _logger.LogWarning(
                        "日志目录所在磁盘剩余百分比低于阈值。RootPath={RootPath}, FreePercent={FreePercent:F2}, ThresholdPercent={ThresholdPercent}",
                        snapshot.RootPath,
                        freePercent,
                        options.LowDiskFreeSpacePercentWarningThreshold);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "评估日志目录容量告警失败。LogDirectory={LogDirectory}", logDirectory);
        }
    }

    private static long GetDirectorySizeBytes(string directory)
    {
        var totalBytes = 0L;
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(directory);

        while (pendingDirectories.Count > 0)
        {
            var currentDirectory = pendingDirectories.Pop();
            try
            {
                foreach (var filePath in Directory.EnumerateFiles(currentDirectory, "*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        totalBytes += new FileInfo(filePath).Length;
                    }
                    catch
                    {
                    }
                }

                foreach (var subDirectory in Directory.EnumerateDirectories(currentDirectory, "*", SearchOption.TopDirectoryOnly))
                {
                    pendingDirectories.Push(subDirectory);
                }
            }
            catch
            {
            }
        }

        return totalBytes;
    }
}

/// <summary>
/// 定义磁盘空间探测能力。
/// </summary>
public interface IDiskSpaceProbe
{
    /// <summary>
    /// 尝试获取指定路径所在磁盘快照。
    /// </summary>
    /// <param name="path">目标路径。</param>
    /// <param name="snapshot">磁盘快照。</param>
    /// <returns>成功时返回 true。</returns>
    bool TryGetSnapshot(string path, out DiskSpaceSnapshot snapshot);
}

/// <summary>
/// 基于 DriveInfo 的磁盘空间探测实现。
/// </summary>
public sealed class DriveInfoDiskSpaceProbe : IDiskSpaceProbe
{
    /// <inheritdoc />
    public bool TryGetSnapshot(string path, out DiskSpaceSnapshot snapshot)
    {
        snapshot = default;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var fullPath = Path.GetFullPath(path);
        var rootPath = Path.GetPathRoot(fullPath);
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return false;
        }

        var drive = new DriveInfo(rootPath);
        if (!drive.IsReady)
        {
            return false;
        }

        snapshot = new DiskSpaceSnapshot(rootPath, drive.AvailableFreeSpace, drive.TotalSize);
        return true;
    }
}

/// <summary>
/// 表示磁盘空间快照。
/// </summary>
/// <param name="RootPath">根路径。</param>
/// <param name="AvailableFreeSpaceBytes">可用空间字节数。</param>
/// <param name="TotalSizeBytes">总容量字节数。</param>
public readonly record struct DiskSpaceSnapshot(
    string RootPath,
    long AvailableFreeSpaceBytes,
    long TotalSizeBytes)
{
    /// <summary>
    /// 获取可用空间 MB。
    /// </summary>
    public decimal AvailableFreeSpaceMb => decimal.Round(
        AvailableFreeSpaceBytes / (decimal)LogFileMaintenanceService.BytesPerMegabyte,
        3);
}

/// <summary>
/// 表示日志清理结果。
/// </summary>
/// <param name="LogDirectory">日志目录。</param>
/// <param name="DeletedCount">删除成功数量。</param>
/// <param name="FailedCount">删除失败数量。</param>
/// <param name="ScanFailedCount">目录扫描失败数量。</param>
public sealed record LogCleanupSummary(
    string LogDirectory,
    int DeletedCount,
    int FailedCount,
    int ScanFailedCount)
{
    /// <summary>
    /// 获取失败总数。
    /// </summary>
    public int TotalFailedCount => FailedCount + ScanFailedCount;
}

/// <summary>
/// 表示启动磁盘门禁结果。
/// </summary>
/// <param name="IsSatisfied">是否满足启动阈值。</param>
/// <param name="CleanupAttempted">是否已尝试清理日志。</param>
/// <param name="IsDisabled">是否已关闭门禁。</param>
/// <param name="BeforeSnapshot">清理前快照。</param>
/// <param name="AfterSnapshot">清理后快照。</param>
/// <param name="CleanupSummary">清理摘要。</param>
/// <param name="FailureReason">失败原因。</param>
public sealed record StartupDiskSpaceGuardResult(
    bool IsSatisfied,
    bool CleanupAttempted,
    bool IsDisabled,
    DiskSpaceSnapshot? BeforeSnapshot,
    DiskSpaceSnapshot? AfterSnapshot,
    LogCleanupSummary? CleanupSummary,
    string? FailureReason)
{
    /// <summary>
    /// 获取禁用门禁结果。
    /// </summary>
    public static StartupDiskSpaceGuardResult Disabled { get; } =
        new(true, false, true, null, null, null, null);

    /// <summary>
    /// 创建无需清理即可通过的结果。
    /// </summary>
    /// <param name="beforeSnapshot">清理前快照。</param>
    /// <returns>检查结果。</returns>
    public static StartupDiskSpaceGuardResult Passed(DiskSpaceSnapshot beforeSnapshot) =>
        new(true, false, false, beforeSnapshot, beforeSnapshot, null, null);

    /// <summary>
    /// 创建清理后恢复的结果。
    /// </summary>
    /// <param name="beforeSnapshot">清理前快照。</param>
    /// <param name="afterSnapshot">清理后快照。</param>
    /// <param name="cleanupSummary">清理摘要。</param>
    /// <returns>检查结果。</returns>
    public static StartupDiskSpaceGuardResult Recovered(
        DiskSpaceSnapshot beforeSnapshot,
        DiskSpaceSnapshot afterSnapshot,
        LogCleanupSummary cleanupSummary) =>
        new(true, true, false, beforeSnapshot, afterSnapshot, cleanupSummary, null);

    /// <summary>
    /// 创建失败结果。
    /// </summary>
    /// <param name="beforeSnapshot">清理前快照。</param>
    /// <param name="afterSnapshot">清理后快照。</param>
    /// <param name="cleanupSummary">清理摘要。</param>
    /// <param name="failureReason">失败原因。</param>
    /// <returns>检查结果。</returns>
    public static StartupDiskSpaceGuardResult Failed(
        DiskSpaceSnapshot beforeSnapshot,
        DiskSpaceSnapshot? afterSnapshot,
        LogCleanupSummary? cleanupSummary,
        string failureReason) =>
        new(false, cleanupSummary is not null, false, beforeSnapshot, afterSnapshot, cleanupSummary, failureReason);

    /// <summary>
    /// 创建检查执行失败结果。
    /// </summary>
    /// <param name="failureReason">失败原因。</param>
    /// <returns>检查结果。</returns>
    public static StartupDiskSpaceGuardResult InspectionFailed(string failureReason) =>
        new(false, false, false, null, null, null, failureReason);
}
