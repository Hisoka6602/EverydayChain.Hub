using System.Diagnostics;
using System.Collections.Concurrent;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public class RuntimeStorageGuard(IOptions<SyncJobOptions> syncJobOptions, ILogger<RuntimeStorageGuard> logger) : IRuntimeStorageGuard
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const long MaxTableMemoryWarningThresholdMb = 65536;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const double DefaultBytesPerEntryEstimate = 1024d;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const long DefaultTableMemoryWarningThresholdMb = 256;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const int DefaultTableMemoryWarningLogIntervalSeconds = 300;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const int MaxTableMemoryWarningLogIntervalSeconds = 86400;

    private readonly string _checkpointFilePath = RuntimeStoragePathResolver.ResolveAbsolutePath(
        syncJobOptions.Value.CheckpointFilePath,
        "sync-checkpoints.json");

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly SyncJobOptions _options = syncJobOptions.Value;

    private readonly Dictionary<string, long> _writeSpaceCheckCache = new(StringComparer.OrdinalIgnoreCase);

    private readonly object _writeSpaceCheckCacheLock = new();

    private readonly object _thresholdLogLock = new();

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private bool _startupThresholdDisabledLogged;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private bool _startupThresholdInvalidLogged;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private bool _writeThresholdDisabledLogged;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private bool _writeThresholdInvalidLogged;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private bool _tableMemoryMonitoringDisabledLogged;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private bool _tableMemoryThresholdDisabledLogged;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private bool _tableMemoryThresholdInvalidLogged;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private bool _tableMemoryThresholdTooLargeLogged;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private bool _tableMemoryWarningIntervalInvalidLogged;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private bool _tableMemoryWarningIntervalTooLargeLogged;

    private readonly ConcurrentDictionary<string, long> _tableMemoryWarningLogTimestamps = new(StringComparer.OrdinalIgnoreCase);

    private readonly object _tableMemoryWarningGateLock = new();

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private long _tableMemoryWarningIntervalTicksCache = -1;

    private readonly object _tableMemoryWarningIntervalTicksCacheLock = new();

    public Task EnsureStartupHealthyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var startupMinFreeSpaceMb = NormalizeStartupMinFreeSpaceMb();
        EnsureDirectoryWritable(_checkpointFilePath, "检查点目录");
        EnsureFileReadableAndWritable(_checkpointFilePath, "检查点文件");
        if (startupMinFreeSpaceMb > 0)
        {
            EnsureDiskFreeSpace(_checkpointFilePath, startupMinFreeSpaceMb, "启动自检-检查点路径");
        }
        logger.LogInformation(
            "运行期存储启动自检通过。CheckpointPath={CheckpointPath}, MinFreeSpaceMb={MinFreeSpaceMb}",
            _checkpointFilePath,
            startupMinFreeSpaceMb);
        return Task.CompletedTask;
    }

    public Task EnsureWriteSpaceAsync(string targetPath, string scene, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var writeMinFreeSpaceMb = NormalizeWriteMinFreeSpaceMb();
        if (writeMinFreeSpaceMb == 0)
        {
            return Task.CompletedTask;
        }

        if (CanSkipWriteSpaceCheck(targetPath))
        {
            return Task.CompletedTask;
        }

        EnsureDiskFreeSpace(targetPath, writeMinFreeSpaceMb, scene);
        UpdateWriteSpaceCheckTime(targetPath);
        return Task.CompletedTask;
    }

    public Task ReportTableMemoryAsync(string tableCode, int entryCount, string scene, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!_options.EnableTableMemoryMonitoring)
        {
            lock (_thresholdLogLock)
            {
                if (!_tableMemoryMonitoringDisabledLogged)
                {
                    logger.LogWarning("单表内存监控已关闭。Option={OptionName}", nameof(_options.EnableTableMemoryMonitoring));
                    _tableMemoryMonitoringDisabledLogged = true;
                }
            }

            return Task.CompletedTask;
        }

        var warningThresholdMb = NormalizeTableMemoryWarningThresholdMb();
        if (warningThresholdMb == 0)
        {
            return Task.CompletedTask;
        }

        var estimatedMemoryMb = EstimateTableMemoryMb(entryCount);
        if (estimatedMemoryMb < warningThresholdMb)
        {
            return Task.CompletedTask;
        }

        if (!TryAcquireTableMemoryWarningLogPermission(tableCode))
        {
            return Task.CompletedTask;
        }

        logger.LogWarning(
            "场景【{Scene}】触发单表内存水位告警。TableCode={TableCode}, EntryCount={EntryCount}, EstimatedMemoryMb={EstimatedMemoryMb:F2}, WarningThresholdMb={WarningThresholdMb}",
            scene,
            tableCode,
            entryCount,
            estimatedMemoryMb,
            warningThresholdMb);
        return Task.CompletedTask;
    }

    private long NormalizeStartupMinFreeSpaceMb()
    {
        if (_options.StartupMinFreeSpaceMb == 0)
        {
            lock (_thresholdLogLock)
            {
                if (!_startupThresholdDisabledLogged)
                {
                    logger.LogWarning("启动自检磁盘阈值已关闭。Option={OptionName}", nameof(_options.StartupMinFreeSpaceMb));
                    _startupThresholdDisabledLogged = true;
                }
            }

            return 0;
        }

        if (_options.StartupMinFreeSpaceMb < 0)
        {
            lock (_thresholdLogLock)
            {
                if (!_startupThresholdInvalidLogged)
                {
                    logger.LogWarning(
                        "启动自检磁盘阈值配置非法，已回退默认值。Option={OptionName}, Value={OptionValue}, DefaultValue={DefaultValue}",
                        nameof(_options.StartupMinFreeSpaceMb),
                        _options.StartupMinFreeSpaceMb,
                        500);
                    _startupThresholdInvalidLogged = true;
                }
            }

            return 500;
        }

        return _options.StartupMinFreeSpaceMb;
    }

    private long NormalizeWriteMinFreeSpaceMb()
    {
        if (_options.WriteMinFreeSpaceMb == 0)
        {
            lock (_thresholdLogLock)
            {
                if (!_writeThresholdDisabledLogged)
                {
                    logger.LogWarning("关键写入磁盘阈值已关闭。Option={OptionName}", nameof(_options.WriteMinFreeSpaceMb));
                    _writeThresholdDisabledLogged = true;
                }
            }

            return 0;
        }

        if (_options.WriteMinFreeSpaceMb < 0)
        {
            lock (_thresholdLogLock)
            {
                if (!_writeThresholdInvalidLogged)
                {
                    logger.LogWarning(
                        "关键写入磁盘阈值配置非法，已回退默认值。Option={OptionName}, Value={OptionValue}, DefaultValue={DefaultValue}",
                        nameof(_options.WriteMinFreeSpaceMb),
                        _options.WriteMinFreeSpaceMb,
                        100);
                    _writeThresholdInvalidLogged = true;
                }
            }

            return 100;
        }

        return _options.WriteMinFreeSpaceMb;
    }

    private long NormalizeTableMemoryWarningThresholdMb()
    {
        if (_options.TableMemoryWarningThresholdMb == 0)
        {
            lock (_thresholdLogLock)
            {
                if (!_tableMemoryThresholdDisabledLogged)
                {
                    logger.LogWarning(
                        "单表内存告警阈值已关闭。Option={OptionName}",
                        nameof(_options.TableMemoryWarningThresholdMb));
                    _tableMemoryThresholdDisabledLogged = true;
                }
            }

            return 0;
        }

        if (_options.TableMemoryWarningThresholdMb < 0)
        {
            lock (_thresholdLogLock)
            {
                if (!_tableMemoryThresholdInvalidLogged)
                {
                    logger.LogWarning(
                        "单表内存告警阈值配置非法，已回退默认值。Option={OptionName}, Value={OptionValue}, DefaultValue={DefaultValue}",
                        nameof(_options.TableMemoryWarningThresholdMb),
                        _options.TableMemoryWarningThresholdMb,
                        DefaultTableMemoryWarningThresholdMb);
                    _tableMemoryThresholdInvalidLogged = true;
                }
            }

            return DefaultTableMemoryWarningThresholdMb;
        }

        if (_options.TableMemoryWarningThresholdMb > MaxTableMemoryWarningThresholdMb)
        {
            lock (_thresholdLogLock)
            {
                if (!_tableMemoryThresholdTooLargeLogged)
                {
                    logger.LogWarning(
                        "单表内存告警阈值超出上限，已钳制为最大值。Option={OptionName}, Value={OptionValue}, MaxValue={MaxValue}",
                        nameof(_options.TableMemoryWarningThresholdMb),
                        _options.TableMemoryWarningThresholdMb,
                        MaxTableMemoryWarningThresholdMb);
                    _tableMemoryThresholdTooLargeLogged = true;
                }
            }

            return MaxTableMemoryWarningThresholdMb;
        }

        return _options.TableMemoryWarningThresholdMb;
    }

    private static double EstimateTableMemoryMb(int entryCount)
    {
        var estimatedBytes = entryCount * DefaultBytesPerEntryEstimate;
        return estimatedBytes / 1024d / 1024d;
    }

    private bool TryAcquireTableMemoryWarningLogPermission(string tableCode)
    {
        var intervalTicks = GetTableMemoryWarningIntervalTicks();
        if (intervalTicks <= 0)
        {
            return true;
        }

        var currentTimestamp = Stopwatch.GetTimestamp();
        lock (_tableMemoryWarningGateLock)
        {
            if (_tableMemoryWarningLogTimestamps.TryGetValue(tableCode, out var lastTimestamp)
                && currentTimestamp - lastTimestamp < intervalTicks)
            {
                return false;
            }

            _tableMemoryWarningLogTimestamps[tableCode] = currentTimestamp;
            return true;
        }
    }

    private long GetTableMemoryWarningIntervalTicks()
    {
        var cached = Interlocked.Read(ref _tableMemoryWarningIntervalTicksCache);
        if (cached >= 0)
        {
            return cached;
        }

        lock (_tableMemoryWarningIntervalTicksCacheLock)
        {
            cached = Interlocked.Read(ref _tableMemoryWarningIntervalTicksCache);
            if (cached >= 0)
            {
                return cached;
            }

            var intervalSeconds = NormalizeTableMemoryWarningLogIntervalSeconds();
            _tableMemoryWarningIntervalTicksCache = intervalSeconds <= 0
                ? 0
                : (long)(intervalSeconds * Stopwatch.Frequency);
            return _tableMemoryWarningIntervalTicksCache;
        }
    }

    private int NormalizeTableMemoryWarningLogIntervalSeconds()
    {
        if (_options.TableMemoryWarningLogIntervalSeconds < 0)
        {
            lock (_thresholdLogLock)
            {
                if (!_tableMemoryWarningIntervalInvalidLogged)
                {
                    logger.LogWarning(
                        "单表内存告警节流间隔配置非法，已回退默认值。Option={OptionName}, Value={OptionValue}, DefaultValue={DefaultValue}",
                        nameof(_options.TableMemoryWarningLogIntervalSeconds),
                        _options.TableMemoryWarningLogIntervalSeconds,
                        DefaultTableMemoryWarningLogIntervalSeconds);
                    _tableMemoryWarningIntervalInvalidLogged = true;
                }
            }

            return DefaultTableMemoryWarningLogIntervalSeconds;
        }

        if (_options.TableMemoryWarningLogIntervalSeconds > MaxTableMemoryWarningLogIntervalSeconds)
        {
            lock (_thresholdLogLock)
            {
                if (!_tableMemoryWarningIntervalTooLargeLogged)
                {
                    logger.LogWarning(
                        "单表内存告警节流间隔超出上限，已钳制为最大值。Option={OptionName}, Value={OptionValue}, MaxValue={MaxValue}",
                        nameof(_options.TableMemoryWarningLogIntervalSeconds),
                        _options.TableMemoryWarningLogIntervalSeconds,
                        MaxTableMemoryWarningLogIntervalSeconds);
                    _tableMemoryWarningIntervalTooLargeLogged = true;
                }
            }

            return MaxTableMemoryWarningLogIntervalSeconds;
        }

        return _options.TableMemoryWarningLogIntervalSeconds;
    }

    private bool CanSkipWriteSpaceCheck(string targetPath)
    {
        var cacheSeconds = _options.WriteSpaceCheckCacheSeconds;
        if (cacheSeconds <= 0)
        {
            return false;
        }

        var currentTimestamp = Stopwatch.GetTimestamp();
        var cacheWindowTicks = (long)(cacheSeconds * Stopwatch.Frequency);
        lock (_writeSpaceCheckCacheLock)
        {
            return _writeSpaceCheckCache.TryGetValue(targetPath, out var lastCheckedAt)
                && currentTimestamp - lastCheckedAt <= cacheWindowTicks;
        }
    }

    private void UpdateWriteSpaceCheckTime(string targetPath)
    {
        lock (_writeSpaceCheckCacheLock)
        {
            _writeSpaceCheckCache[targetPath] = Stopwatch.GetTimestamp();
        }
    }

    private void EnsureDirectoryWritable(string targetFilePath, string scene)
    {
        var directoryPath = Path.GetDirectoryName(targetFilePath);
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            logger.LogError("{Scene}失败：目标目录为空。TargetFilePath={TargetFilePath}", scene, targetFilePath);
            throw new InvalidOperationException($"{scene}失败：目标目录为空。Path={targetFilePath}");
        }

        Directory.CreateDirectory(directoryPath);
        var probePath = Path.Combine(directoryPath, $".write-probe-{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(probePath, "probe");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Scene}失败：目录不可写。Directory={DirectoryPath}", scene, directoryPath);
            throw new InvalidOperationException($"{scene}失败：目录不可写。Directory={directoryPath}", ex);
        }
        finally
        {
            try
            {
                if (File.Exists(probePath))
                {
                    File.Delete(probePath);
                }
            }
            catch (Exception cleanupEx)
            {
                logger.LogError(cleanupEx, "{Scene}失败：目录探针文件清理失败。ProbePath={ProbePath}", scene, probePath);
            }
        }
    }

    private void EnsureFileReadableAndWritable(string targetFilePath, string scene)
    {
        try
        {
            if (!File.Exists(targetFilePath))
            {
                return;
            }

            using var readStream = File.Open(targetFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (readStream.Length > 0)
            {
                _ = readStream.ReadByte();
            }
            using var writeStream = File.Open(targetFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            writeStream.Flush();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Scene}失败：文件不可读写。Path={Path}", scene, targetFilePath);
            throw new InvalidOperationException($"{scene}失败：文件不可读写。Path={targetFilePath}", ex);
        }
    }

    private void EnsureDiskFreeSpace(string targetFilePath, long minFreeSpaceMb, string scene)
    {
        string? rootPath = Path.GetPathRoot(Path.GetFullPath(targetFilePath));
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            logger.LogError("{Scene}失败：无法解析磁盘根路径。Path={Path}", scene, targetFilePath);
            throw new InvalidOperationException($"{scene}失败：无法解析磁盘根路径。Path={targetFilePath}");
        }

        try
        {
            var driveInfo = new DriveInfo(rootPath);
            var freeSpaceMb = driveInfo.AvailableFreeSpace / 1024d / 1024d;
            if (freeSpaceMb >= minFreeSpaceMb)
            {
                return;
            }

            logger.LogError(
                "{Scene}失败：磁盘可用空间不足。Path={Path}, Root={Root}, FreeSpaceMb={FreeSpaceMb:F2}, ThresholdMb={ThresholdMb}",
                scene,
                targetFilePath,
                rootPath,
                freeSpaceMb,
                minFreeSpaceMb);
            throw new InvalidOperationException(
                $"{scene}失败：磁盘可用空间不足。Path={targetFilePath}, Root={rootPath}, FreeSpaceMb={freeSpaceMb:F2}, ThresholdMb={minFreeSpaceMb}");
        }
        catch (IOException ex)
        {
            throw CreateDiskSpaceValidationException(ex, scene, targetFilePath, rootPath, minFreeSpaceMb);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw CreateDiskSpaceValidationException(ex, scene, targetFilePath, rootPath, minFreeSpaceMb);
        }
        catch (ArgumentException ex)
        {
            throw CreateDiskSpaceValidationException(ex, scene, targetFilePath, rootPath, minFreeSpaceMb);
        }
        catch (NotSupportedException ex)
        {
            throw CreateDiskSpaceValidationException(ex, scene, targetFilePath, rootPath, minFreeSpaceMb);
        }
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private InvalidOperationException CreateDiskSpaceValidationException(
        Exception exception,
        string scene,
        string targetFilePath,
        string? rootPath,
        long minFreeSpaceMb)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        logger.LogError(
            exception,
            "{Scene}失败：磁盘可用空间校验异常。Path={Path}, Root={Root}, ThresholdMb={ThresholdMb}",
            scene,
            targetFilePath,
            rootPath,
            minFreeSpaceMb);
        return new InvalidOperationException(
            $"{scene}失败：磁盘可用空间校验异常。Path={targetFilePath}, Root={rootPath ?? "N/A"}, ThresholdMb={minFreeSpaceMb}",
            exception);
    }
}

