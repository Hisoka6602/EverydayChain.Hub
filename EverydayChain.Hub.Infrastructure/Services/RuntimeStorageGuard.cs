using System.Diagnostics;
using System.Collections.Concurrent;
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
    /// <summary>单表内存告警阈值上限（MB）。</summary>
    private const long MaxTableMemoryWarningThresholdMb = 65536;

    /// <summary>单表内存估算默认每条字节数（单位：Byte）。</summary>
    private const double DefaultBytesPerEntryEstimate = 1024d;

    /// <summary>单表内存告警默认阈值（MB）。</summary>
    private const long DefaultTableMemoryWarningThresholdMb = 256;

    /// <summary>单表内存告警节流间隔默认值（秒）。</summary>
    private const int DefaultTableMemoryWarningLogIntervalSeconds = 300;

    /// <summary>单表内存告警节流间隔上限（秒）。</summary>
    private const int MaxTableMemoryWarningLogIntervalSeconds = 86400;

    /// <summary>检查点文件绝对路径（由配置项 CheckpointFilePath 决定；为空时使用应用基目录下 sync-checkpoints.json）。</summary>
    private readonly string _checkpointFilePath = RuntimeStoragePathResolver.ResolveAbsolutePath(
        syncJobOptions.Value.CheckpointFilePath,
        "sync-checkpoints.json");

    /// <summary>批次文件绝对路径（由配置项 BatchFilePath 决定；为空时使用应用基目录下 data/sync-batches.json）。</summary>
    private readonly string _batchFilePath = RuntimeStoragePathResolver.ResolveAbsolutePath(
        syncJobOptions.Value.BatchFilePath,
        "data/sync-batches.json");

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

    /// <summary>单表内存阈值超上限日志是否已输出。</summary>
    private bool _tableMemoryThresholdTooLargeLogged;

    /// <summary>单表内存告警节流配置非法日志是否已输出。</summary>
    private bool _tableMemoryWarningIntervalInvalidLogged;

    /// <summary>单表内存告警节流配置超上限日志是否已输出。</summary>
    private bool _tableMemoryWarningIntervalTooLargeLogged;

    /// <summary>单表内存告警最近输出时间缓存（Stopwatch 时间戳）。</summary>
    private readonly ConcurrentDictionary<string, long> _tableMemoryWarningLogTimestamps = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>单表内存告警判定与时间戳更新锁。</summary>
    private readonly object _tableMemoryWarningGateLock = new();

    /// <summary>
    /// 单表内存告警节流间隔缓存（Stopwatch Tick）。
    /// 初始值 -1 表示尚未初始化；读取时须通过 <see cref="Interlocked.Read"/> 保证跨线程可见性（long 不支持 volatile）。
    /// </summary>
    private long _tableMemoryWarningIntervalTicksCache = -1;

    /// <summary>单表内存告警节流间隔缓存锁。</summary>
    private readonly object _tableMemoryWarningIntervalTicksCacheLock = new();

    /// <inheritdoc/>
    public Task EnsureStartupHealthyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var startupMinFreeSpaceMb = NormalizeStartupMinFreeSpaceMb();
        EnsureDirectoryWritable(_checkpointFilePath, "检查点目录");
        EnsureFileReadableAndWritable(_checkpointFilePath, "检查点文件");
        EnsureDirectoryWritable(_batchFilePath, "批次目录");
        EnsureFileReadableAndWritable(_batchFilePath, "批次文件");
        if (startupMinFreeSpaceMb > 0)
        {
            EnsureDiskFreeSpace(_checkpointFilePath, startupMinFreeSpaceMb, "启动自检-检查点路径");
            EnsureDiskFreeSpace(_batchFilePath, startupMinFreeSpaceMb, "启动自检-批次路径");
        }
        logger.LogInformation(
            "运行期存储启动自检通过。CheckpointPath={CheckpointPath}, BatchPath={BatchPath}, MinFreeSpaceMb={MinFreeSpaceMb}",
            _checkpointFilePath,
            _batchFilePath,
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

    /// <summary>
    /// 估算单表内存占用（MB）。
    /// </summary>
    /// <param name="entryCount">条目数量。</param>
    /// <returns>估算内存（MB）。</returns>
    private static double EstimateTableMemoryMb(int entryCount)
    {
        // 保守估算：按每条记录约 1KB（1024 Byte）计算，适用于键+字段字典常见场景。
        // 该值用于告警预估而非精确计量；若单条字段显著增多或大字段占比提升，应结合实测调整。
        var estimatedBytes = entryCount * DefaultBytesPerEntryEstimate;
        return estimatedBytes / 1024d / 1024d;
    }

    /// <summary>
    /// 判断并获取单表内存告警日志输出许可（原子执行）。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    /// <returns>允许输出返回 <c>true</c>。</returns>
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

    /// <summary>
    /// 获取单表内存告警节流间隔（Stopwatch Tick）。
    /// </summary>
    /// <returns>节流间隔 Tick（0 表示不节流）。</returns>
    private long GetTableMemoryWarningIntervalTicks()
    {
        // 首次检查使用 Interlocked.Read 将值读入局部变量，保证 64 位读取的原子性与 Acquire 内存屏障，
        // 避免 CPU/JIT 缓存导致 DCL 首检读到过期值（long 不支持 volatile）。
        var cached = Interlocked.Read(ref _tableMemoryWarningIntervalTicksCache);
        if (cached >= 0)
        {
            return cached;
        }

        lock (_tableMemoryWarningIntervalTicksCacheLock)
        {
            // 锁内二次检查同样读入局部变量，与首检保持语义一致，避免混用同步原语引发维护困惑。
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

    /// <summary>
    /// 获取规范化后的单表内存告警节流间隔。
    /// </summary>
    /// <returns>节流间隔（秒）。</returns>
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
