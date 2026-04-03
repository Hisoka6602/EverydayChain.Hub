using System.Diagnostics;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 运行期存储守护服务。
/// </summary>
public class RuntimeStorageGuard(IOptions<SyncJobOptions> syncJobOptions, ILogger<RuntimeStorageGuard> logger) : IRuntimeStorageGuard
{
    /// <summary>检查点文件绝对路径。</summary>
    private readonly string _checkpointFilePath = RuntimeStoragePathResolver.ResolveAbsolutePath(
        syncJobOptions.Value.CheckpointFilePath,
        "sync-checkpoints.json");

    /// <summary>目标端快照文件绝对路径。</summary>
    private readonly string _targetStoreFilePath = RuntimeStoragePathResolver.ResolveAbsolutePath(
        syncJobOptions.Value.TargetStoreFilePath,
        Path.Combine("data", "sync-target-store.json"));

    /// <summary>运行期配置。</summary>
    private readonly SyncJobOptions _options = syncJobOptions.Value;

    /// <summary>关键写入磁盘检查缓存。</summary>
    private readonly Dictionary<string, long> _writeSpaceCheckCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>关键写入磁盘检查缓存锁。</summary>
    private readonly object _writeSpaceCheckCacheLock = new();

    /// <summary>阈值日志锁。</summary>
    private readonly object _thresholdLogLock = new();

    /// <summary>启动阈值关闭日志是否已输出。</summary>
    private bool _startupThresholdDisabledLogged;

    /// <summary>启动阈值非法日志是否已输出。</summary>
    private bool _startupThresholdInvalidLogged;

    /// <summary>写入阈值关闭日志是否已输出。</summary>
    private bool _writeThresholdDisabledLogged;

    /// <summary>写入阈值非法日志是否已输出。</summary>
    private bool _writeThresholdInvalidLogged;

    /// <summary>单表内存监控关闭日志是否已输出。</summary>
    private bool _tableMemoryMonitoringDisabledLogged;

    /// <summary>单表内存阈值关闭日志是否已输出。</summary>
    private bool _tableMemoryThresholdDisabledLogged;

    /// <summary>单表内存阈值非法日志是否已输出。</summary>
    private bool _tableMemoryThresholdInvalidLogged;

    /// <inheritdoc/>
    public Task EnsureStartupHealthyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var startupMinFreeSpaceMb = NormalizeStartupMinFreeSpaceMb();
        EnsureDirectoryWritable(_checkpointFilePath, "检查点目录");
        EnsureDirectoryWritable(_targetStoreFilePath, "目标快照目录");
        EnsureFileReadableAndWritable(_checkpointFilePath, "检查点文件");
        EnsureFileReadableAndWritable(_targetStoreFilePath, "目标快照文件");
        if (startupMinFreeSpaceMb > 0)
        {
            EnsureDiskFreeSpace(_checkpointFilePath, startupMinFreeSpaceMb, "启动自检-检查点路径");
            EnsureDiskFreeSpace(_targetStoreFilePath, startupMinFreeSpaceMb, "启动自检-目标快照路径");
        }
        logger.LogInformation(
            "运行期存储启动自检通过。CheckpointPath={CheckpointPath}, TargetStorePath={TargetStorePath}, MinFreeSpaceMb={MinFreeSpaceMb}",
            _checkpointFilePath,
            _targetStoreFilePath,
            startupMinFreeSpaceMb);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

        logger.LogWarning(
            "{Scene}触发单表内存水位告警。TableCode={TableCode}, EntryCount={EntryCount}, EstimatedMemoryMb={EstimatedMemoryMb:F2}, WarningThresholdMb={WarningThresholdMb}",
            scene,
            tableCode,
            entryCount,
            estimatedMemoryMb,
            warningThresholdMb);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 获取规范化后的启动最小可用空间。
    /// </summary>
    /// <returns>最小可用空间（MB）。</returns>
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

    /// <summary>
    /// 获取规范化后的关键写入最小可用空间。
    /// </summary>
    /// <returns>最小可用空间（MB）。</returns>
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

    /// <summary>
    /// 获取规范化后的单表内存告警阈值。
    /// </summary>
    /// <returns>阈值（MB）。</returns>
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
                        256);
                    _tableMemoryThresholdInvalidLogged = true;
                }
            }

            return 256;
        }

        return _options.TableMemoryWarningThresholdMb;
    }

    /// <summary>
    /// 估算单表内存占用（MB）。
    /// </summary>
    /// <param name="entryCount">条目数量。</param>
    /// <returns>估算内存（MB）。</returns>
    private static double EstimateTableMemoryMb(int entryCount)
    {
        // 保守估算：每条约 1KB，用于触发预警而非精确计量。
        const double bytesPerEntryEstimate = 1024d;
        var estimatedBytes = entryCount * bytesPerEntryEstimate;
        return estimatedBytes / 1024d / 1024d;
    }

    /// <summary>
    /// 判断是否可跳过本次关键写入磁盘检查。
    /// </summary>
    /// <param name="targetPath">目标路径。</param>
    /// <returns>可跳过返回 <c>true</c>。</returns>
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

    /// <summary>
    /// 更新关键写入磁盘检查时间戳。
    /// </summary>
    /// <param name="targetPath">目标路径。</param>
    private void UpdateWriteSpaceCheckTime(string targetPath)
    {
        lock (_writeSpaceCheckCacheLock)
        {
            _writeSpaceCheckCache[targetPath] = Stopwatch.GetTimestamp();
        }
    }

    /// <summary>
    /// 校验目录可写。
    /// </summary>
    /// <param name="targetFilePath">目标文件路径。</param>
    /// <param name="scene">场景描述。</param>
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

    /// <summary>
    /// 校验文件可读写。
    /// </summary>
    /// <param name="targetFilePath">目标文件路径。</param>
    /// <param name="scene">场景描述。</param>
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

    /// <summary>
    /// 校验磁盘可用空间。
    /// </summary>
    /// <param name="targetFilePath">目标文件路径。</param>
    /// <param name="minFreeSpaceMb">最小可用空间（MB）。</param>
    /// <param name="scene">场景描述。</param>
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
    /// 创建磁盘可用空间校验异常并输出错误日志。
    /// </summary>
    /// <param name="exception">原始异常。</param>
    /// <param name="scene">场景描述。</param>
    /// <param name="targetFilePath">目标文件路径。</param>
    /// <param name="rootPath">磁盘根路径。</param>
    /// <param name="minFreeSpaceMb">最小可用空间。</param>
    /// <returns>包装后的异常。</returns>
    private InvalidOperationException CreateDiskSpaceValidationException(
        Exception exception,
        string scene,
        string targetFilePath,
        string? rootPath,
        long minFreeSpaceMb)
    {
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
