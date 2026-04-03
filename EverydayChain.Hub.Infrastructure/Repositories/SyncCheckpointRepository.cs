using System.Text.Json;
using EverydayChain.Hub.Application.Repositories;
using EverydayChain.Hub.Domain.Sync;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EverydayChain.Hub.Infrastructure.Options;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 同步检查点仓储基础实现（文件持久化）。
/// </summary>
public class SyncCheckpointRepository(IOptions<SyncJobOptions> syncJobOptions, ILogger<SyncCheckpointRepository> logger) : ISyncCheckpointRepository
{
    /// <summary>文件访问锁。</summary>
    private static readonly SemaphoreSlim FileLock = new(1, 1);

    /// <summary>检查点 JSON 序列化配置（缩进输出，复用避免重复分配）。</summary>
    private static readonly JsonSerializerOptions CheckpointSerializerOptions = new() { WriteIndented = true };

    /// <summary>检查点文件路径。</summary>
    private readonly string _checkpointFilePath = ResolveCheckpointFilePath(syncJobOptions.Value.CheckpointFilePath);

    /// <summary>
    /// 解析检查点文件路径。
    /// </summary>
    /// <param name="configuredPath">配置路径。</param>
    /// <returns>可用路径。</returns>
    private static string ResolveCheckpointFilePath(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.Combine(AppContext.BaseDirectory, "sync-checkpoints.json");
        }

        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, configuredPath);
    }

    /// <inheritdoc/>
    public async Task<SyncCheckpoint> GetAsync(string tableCode, CancellationToken ct)
    {
        logger.LogInformation("读取同步检查点。Path={CheckpointFilePath}, TableCode={TableCode}", _checkpointFilePath, tableCode);
        var checkpoints = await LoadAllAsync(ct);
        if (checkpoints.TryGetValue(tableCode, out var checkpoint))
        {
            return checkpoint;
        }

        return new SyncCheckpoint
        {
            TableCode = tableCode,
        };
    }

    /// <inheritdoc/>
    public async Task SaveAsync(SyncCheckpoint checkpoint, CancellationToken ct)
    {
        logger.LogInformation("开始写入同步检查点。Path={CheckpointFilePath}, TableCode={TableCode}, BatchId={BatchId}",
            _checkpointFilePath,
            checkpoint.TableCode,
            checkpoint.LastBatchId);
        await FileLock.WaitAsync(ct);
        try
        {
            var directory = Path.GetDirectoryName(_checkpointFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var checkpoints = await LoadAllWithoutLockAsync(ct);
            checkpoints[checkpoint.TableCode] = checkpoint;
            var json = JsonSerializer.Serialize(checkpoints, CheckpointSerializerOptions);

            // 原子写入：先落临时文件，再通过 File.Replace/Move 替换，避免进程崩溃时产生半写 JSON。
            var tempFilePath = $"{_checkpointFilePath}.tmp";
            var backupFilePath = $"{_checkpointFilePath}.bak";
            await WriteAtomicAsync(tempFilePath, backupFilePath, json, ct);

            logger.LogInformation("写入同步检查点成功。Path={CheckpointFilePath}, TableCode={TableCode}, BatchId={BatchId}",
                _checkpointFilePath,
                checkpoint.TableCode,
                checkpoint.LastBatchId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "写入检查点文件失败。Path={CheckpointFilePath}", _checkpointFilePath);
            throw;
        }
        finally
        {
            FileLock.Release();
        }
    }

    /// <summary>
    /// 将 JSON 内容原子写入目标文件（写临时文件后替换）。
    /// 写入失败时尝试清理临时文件并重新抛出原始异常。
    /// </summary>
    /// <param name="tempFilePath">临时文件路径。</param>
    /// <param name="backupFilePath">替换备份文件路径。</param>
    /// <param name="json">待写入内容。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task WriteAtomicAsync(string tempFilePath, string backupFilePath, string json, CancellationToken ct)
    {
        try
        {
            await File.WriteAllTextAsync(tempFilePath, json, ct);
            if (File.Exists(_checkpointFilePath))
            {
                File.Replace(tempFilePath, _checkpointFilePath, backupFilePath, ignoreMetadataErrors: false);
                if (File.Exists(backupFilePath))
                {
                    File.Delete(backupFilePath);
                }
            }
            else
            {
                File.Move(tempFilePath, _checkpointFilePath);
            }
        }
        catch
        {
            try
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
            catch (Exception cleanupEx)
            {
                logger.LogError(cleanupEx, "清理检查点临时文件失败。TempPath={TempFilePath}", tempFilePath);
            }
            throw;
        }
    }

    /// <summary>
    /// 加载全部检查点。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>检查点字典。</returns>
    private async Task<Dictionary<string, SyncCheckpoint>> LoadAllAsync(CancellationToken ct)
    {
        await FileLock.WaitAsync(ct);
        try
        {
            return await LoadAllWithoutLockAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "读取检查点文件失败。Path={CheckpointFilePath}", _checkpointFilePath);
            throw;
        }
        finally
        {
            FileLock.Release();
        }
    }

    /// <summary>
    /// 在外部已持锁前提下读取全部检查点。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>检查点字典。</returns>
    private async Task<Dictionary<string, SyncCheckpoint>> LoadAllWithoutLockAsync(CancellationToken ct)
    {
        if (!File.Exists(_checkpointFilePath))
        {
            return new Dictionary<string, SyncCheckpoint>(StringComparer.OrdinalIgnoreCase);
        }

        var json = await File.ReadAllTextAsync(_checkpointFilePath, ct);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, SyncCheckpoint>(StringComparer.OrdinalIgnoreCase);
        }

        var data = JsonSerializer.Deserialize<Dictionary<string, SyncCheckpoint>>(json) ?? new Dictionary<string, SyncCheckpoint>(StringComparer.OrdinalIgnoreCase);
        return new Dictionary<string, SyncCheckpoint>(data, StringComparer.OrdinalIgnoreCase);
    }
}
