using EverydayChain.Hub.Application.Abstractions.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Infrastructure.Services;
using EverydayChain.Hub.SharedKernel.Utilities;
using Newtonsoft.Json;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncCheckpointRepository(
    IOptions<SyncJobOptions> syncJobOptions,
    IRuntimeStorageGuard runtimeStorageGuard,
    ILogger<SyncCheckpointRepository> logger) : ISyncCheckpointRepository
{
    private static readonly SemaphoreSlim FileLock = new(1, 1);

    private static readonly JsonSerializerSettings CheckpointSerializerSettings = new()
    {
        Formatting = Formatting.Indented
    };

    private readonly string _checkpointFilePath = RuntimeStoragePathResolver.ResolveAbsolutePath(
        syncJobOptions.Value.CheckpointFilePath,
        "sync-checkpoints.json");

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

    public async Task SaveAsync(SyncCheckpoint checkpoint, CancellationToken ct)
    {
        logger.LogInformation("开始写入同步检查点。Path={CheckpointFilePath}, TableCode={TableCode}, BatchId={BatchId}",
            _checkpointFilePath,
            checkpoint.TableCode,
            checkpoint.LastBatchId);
        await runtimeStorageGuard.EnsureWriteSpaceAsync(_checkpointFilePath, "检查点写入", ct);
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
            var json = JsonConvert.SerializeObject(checkpoints, CheckpointSerializerSettings);

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

        var data = JsonConvert.DeserializeObject<Dictionary<string, SyncCheckpoint>>(json) ?? new Dictionary<string, SyncCheckpoint>(StringComparer.OrdinalIgnoreCase);
        return new Dictionary<string, SyncCheckpoint>(data, StringComparer.OrdinalIgnoreCase);
    }
}

