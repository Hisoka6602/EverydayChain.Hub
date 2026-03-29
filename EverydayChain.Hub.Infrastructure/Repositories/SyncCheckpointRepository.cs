using System.Text.Json;
using EverydayChain.Hub.Application.Repositories;
using EverydayChain.Hub.Domain.Sync;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 同步检查点仓储基础实现（文件持久化）。
/// </summary>
public class SyncCheckpointRepository(ILogger<SyncCheckpointRepository> logger) : ISyncCheckpointRepository
{
    /// <summary>文件访问锁。</summary>
    private static readonly SemaphoreSlim FileLock = new(1, 1);

    /// <summary>检查点文件路径。</summary>
    private readonly string _checkpointFilePath = Path.Combine(AppContext.BaseDirectory, "sync-checkpoints.json");

    /// <inheritdoc/>
    public async Task<SyncCheckpoint> GetAsync(string tableCode, CancellationToken ct)
    {
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
        await FileLock.WaitAsync(ct);
        try
        {
            var checkpoints = await LoadAllWithoutLockAsync(ct);
            checkpoints[checkpoint.TableCode] = checkpoint;
            var json = JsonSerializer.Serialize(checkpoints, new JsonSerializerOptions
            {
                WriteIndented = true,
            });
            await File.WriteAllTextAsync(_checkpointFilePath, json, ct);
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
            return new Dictionary<string, SyncCheckpoint>(StringComparer.OrdinalIgnoreCase);
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
