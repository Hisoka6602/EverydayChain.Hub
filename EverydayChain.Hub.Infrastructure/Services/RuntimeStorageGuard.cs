using System.Diagnostics;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 运行期存储守护服务。
/// </summary>
public class RuntimeStorageGuard(IOptions<SyncJobOptions> syncJobOptions, ILogger<RuntimeStorageGuard> logger) : IRuntimeStorageGuard
{
    /// <summary>检查点文件绝对路径。</summary>
    private readonly string _checkpointFilePath = ResolvePath(syncJobOptions.Value.CheckpointFilePath, "sync-checkpoints.json");

    /// <summary>目标端快照文件绝对路径。</summary>
    private readonly string _targetStoreFilePath = ResolvePath(syncJobOptions.Value.TargetStoreFilePath, Path.Combine("data", "sync-target-store.json"));

    /// <summary>运行期配置。</summary>
    private readonly SyncJobOptions _options = syncJobOptions.Value;

    /// <summary>关键写入磁盘检查缓存。</summary>
    private readonly Dictionary<string, long> _writeSpaceCheckCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>关键写入磁盘检查缓存锁。</summary>
    private readonly object _writeSpaceCheckCacheLock = new();

    /// <inheritdoc/>
    public Task EnsureStartupHealthyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        EnsureDirectoryWritable(_checkpointFilePath, "检查点目录");
        EnsureDirectoryWritable(_targetStoreFilePath, "目标快照目录");
        EnsureFileReadableAndWritable(_checkpointFilePath, "检查点文件");
        EnsureFileReadableAndWritable(_targetStoreFilePath, "目标快照文件");
        EnsureDiskFreeSpace(_checkpointFilePath, NormalizeStartupMinFreeSpaceMb(), "启动自检-检查点路径");
        EnsureDiskFreeSpace(_targetStoreFilePath, NormalizeStartupMinFreeSpaceMb(), "启动自检-目标快照路径");
        logger.LogInformation(
            "运行期存储启动自检通过。CheckpointPath={CheckpointPath}, TargetStorePath={TargetStorePath}, MinFreeSpaceMb={MinFreeSpaceMb}",
            _checkpointFilePath,
            _targetStoreFilePath,
            NormalizeStartupMinFreeSpaceMb());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task EnsureWriteSpaceAsync(string targetPath, string scene, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (CanSkipWriteSpaceCheck(targetPath))
        {
            return Task.CompletedTask;
        }

        EnsureDiskFreeSpace(targetPath, NormalizeWriteMinFreeSpaceMb(), scene);
        UpdateWriteSpaceCheckTime(targetPath);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 获取规范化后的启动最小可用空间。
    /// </summary>
    /// <returns>最小可用空间（MB）。</returns>
    private long NormalizeStartupMinFreeSpaceMb()
    {
        return _options.StartupMinFreeSpaceMb > 0 ? _options.StartupMinFreeSpaceMb : 500;
    }

    /// <summary>
    /// 获取规范化后的关键写入最小可用空间。
    /// </summary>
    /// <returns>最小可用空间（MB）。</returns>
    private long NormalizeWriteMinFreeSpaceMb()
    {
        return _options.WriteMinFreeSpaceMb > 0 ? _options.WriteMinFreeSpaceMb : 100;
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
    /// 解析配置路径为绝对路径。
    /// </summary>
    /// <param name="configuredPath">配置路径。</param>
    /// <param name="defaultRelativePath">默认相对路径。</param>
    /// <returns>绝对路径。</returns>
    private static string ResolvePath(string configuredPath, string defaultRelativePath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.Combine(AppContext.BaseDirectory, defaultRelativePath);
        }

        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, configuredPath);
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
        var rootPath = Path.GetPathRoot(Path.GetFullPath(targetFilePath));
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new InvalidOperationException($"{scene}失败：无法解析磁盘根路径。Path={targetFilePath}");
        }

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
}
