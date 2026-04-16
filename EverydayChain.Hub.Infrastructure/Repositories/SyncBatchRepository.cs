using System.Text.Json;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Infrastructure.Services;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 同步批次仓储文件持久化实现。
/// </summary>
public class SyncBatchRepository(
    IOptions<SyncJobOptions> syncJobOptions,
    IRuntimeStorageGuard runtimeStorageGuard,
    ILogger<SyncBatchRepository> logger) : ISyncBatchRepository
{
    /// <summary>批次文件访问锁。</summary>
    private static readonly SemaphoreSlim FileLock = new(1, 1);

    /// <summary>批次 JSON 序列化配置。</summary>
    private static readonly JsonSerializerOptions BatchSerializerOptions = new() { WriteIndented = true };

    /// <summary>批次文件路径（由配置项 BatchFilePath 决定；为空时使用应用基目录下 data/sync-batches.json）。</summary>
    private readonly string _batchFilePath = RuntimeStoragePathResolver.ResolveAbsolutePath(
        syncJobOptions.Value.BatchFilePath,
        "data/sync-batches.json");

    /// <inheritdoc/>
    public async Task CreateBatchAsync(SyncBatch batch, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            await FileLock.WaitAsync(ct);
            try
            {
                ValidateBatchForCreate(batch);
                var batches = await LoadAllWithoutLockAsync(ct);
                if (batches.ContainsKey(batch.BatchId))
                {
                    throw new InvalidOperationException($"批次已存在：{batch.BatchId}");
                }

                batch.Status = SyncBatchStatus.Pending;
                batches[batch.BatchId] = CloneBatch(batch);
                await PersistWithoutLockAsync(batches, "创建同步批次", ct);
            }
            finally
            {
                FileLock.Release();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "创建同步批次失败。Path={BatchFilePath}, BatchId={BatchId}, TableCode={TableCode}", _batchFilePath, batch.BatchId, batch.TableCode);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task MarkInProgressAsync(string batchId, DateTime startedTimeLocal, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            await FileLock.WaitAsync(ct);
            try
            {
                var batches = await LoadAllWithoutLockAsync(ct);
                var batch = GetRequiredBatch(batches, batchId);
                batch.Status = SyncBatchStatus.InProgress;
                batch.StartedTimeLocal = startedTimeLocal;
                batches[batchId] = batch;
                await PersistWithoutLockAsync(batches, "更新批次执行状态", ct);
            }
            finally
            {
                FileLock.Release();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "标记批次执行中失败。Path={BatchFilePath}, BatchId={BatchId}", _batchFilePath, batchId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task CompleteBatchAsync(SyncBatchResult result, DateTime completedTimeLocal, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            await FileLock.WaitAsync(ct);
            try
            {
                var batches = await LoadAllWithoutLockAsync(ct);
                var batch = GetRequiredBatch(batches, result.BatchId);
                batch.Status = SyncBatchStatus.Completed;
                batch.CompletedTimeLocal = completedTimeLocal;
                batch.ReadCount = result.ReadCount;
                batch.InsertCount = result.InsertCount;
                batch.UpdateCount = result.UpdateCount;
                batch.DeleteCount = result.DeleteCount;
                batch.SkipCount = result.SkipCount;
                batch.ErrorMessage = null;
                batches[result.BatchId] = batch;
                await PersistWithoutLockAsync(batches, "完成同步批次", ct);
            }
            finally
            {
                FileLock.Release();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "标记批次完成失败。Path={BatchFilePath}, BatchId={BatchId}", _batchFilePath, result.BatchId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task FailBatchAsync(string batchId, string errorMessage, DateTime failedTimeLocal, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            await FileLock.WaitAsync(ct);
            try
            {
                var batches = await LoadAllWithoutLockAsync(ct);
                var batch = GetRequiredBatch(batches, batchId);
                batch.Status = SyncBatchStatus.Failed;
                batch.CompletedTimeLocal = failedTimeLocal;
                batch.ErrorMessage = errorMessage;
                batches[batchId] = batch;
                await PersistWithoutLockAsync(batches, "标记同步批次失败", ct);
            }
            finally
            {
                FileLock.Release();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "标记批次失败失败。Path={BatchFilePath}, BatchId={BatchId}", _batchFilePath, batchId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetLatestFailedBatchIdAsync(string tableCode, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            await FileLock.WaitAsync(ct);
            try
            {
                var batches = await LoadAllWithoutLockAsync(ct);
                var latestFailedBatch = batches.Values
                    .Where(batch => string.Equals(batch.TableCode, tableCode, StringComparison.OrdinalIgnoreCase))
                    .Where(static batch => batch.Status == SyncBatchStatus.Failed)
                    .OrderByDescending(static batch => batch.CompletedTimeLocal ?? DateTime.MinValue)
                    .FirstOrDefault();
                return latestFailedBatch?.BatchId;
            }
            finally
            {
                FileLock.Release();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "查询最近失败批次失败。Path={BatchFilePath}, TableCode={TableCode}", _batchFilePath, tableCode);
            throw;
        }
    }

    /// <summary>
    /// 校验创建批次入参。
    /// </summary>
    /// <param name="batch">批次对象。</param>
    private static void ValidateBatchForCreate(SyncBatch batch)
    {
        if (string.IsNullOrWhiteSpace(batch.BatchId))
        {
            throw new InvalidOperationException("BatchId 不能为空。");
        }

        if (string.IsNullOrWhiteSpace(batch.TableCode))
        {
            throw new InvalidOperationException("TableCode 不能为空。");
        }
    }

    /// <summary>
    /// 获取必须存在的批次。
    /// </summary>
    /// <param name="batches">批次集合。</param>
    /// <param name="batchId">批次编号。</param>
    /// <returns>批次对象。</returns>
    private static SyncBatch GetRequiredBatch(Dictionary<string, SyncBatch> batches, string batchId)
    {
        if (batches.TryGetValue(batchId, out var batch))
        {
            return CloneBatch(batch);
        }

        throw new InvalidOperationException($"未找到批次：{batchId}");
    }

    /// <summary>
    /// 在已持锁状态下持久化批次集合。
    /// </summary>
    /// <param name="batches">批次集合。</param>
    /// <param name="scene">场景名称。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task PersistWithoutLockAsync(Dictionary<string, SyncBatch> batches, string scene, CancellationToken ct)
    {
        await runtimeStorageGuard.EnsureWriteSpaceAsync(_batchFilePath, scene, ct);
        var directory = Path.GetDirectoryName(_batchFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(batches, BatchSerializerOptions);
        var tempFilePath = $"{_batchFilePath}.tmp";
        var backupFilePath = $"{_batchFilePath}.bak";
        await WriteAtomicAsync(tempFilePath, backupFilePath, json, ct);
    }

    /// <summary>
    /// 将 JSON 内容原子写入目标文件（写临时文件后替换）。
    /// </summary>
    /// <param name="tempFilePath">临时文件路径。</param>
    /// <param name="backupFilePath">备份文件路径。</param>
    /// <param name="json">JSON 内容。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task WriteAtomicAsync(string tempFilePath, string backupFilePath, string json, CancellationToken ct)
    {
        try
        {
            await File.WriteAllTextAsync(tempFilePath, json, ct);
            if (File.Exists(_batchFilePath))
            {
                File.Replace(tempFilePath, _batchFilePath, backupFilePath, ignoreMetadataErrors: false);
                if (File.Exists(backupFilePath))
                {
                    File.Delete(backupFilePath);
                }
            }
            else
            {
                File.Move(tempFilePath, _batchFilePath);
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
                logger.LogError(cleanupEx, "清理批次临时文件失败。TempPath={TempFilePath}", tempFilePath);
            }
            throw;
        }
    }

    /// <summary>
    /// 在已持锁状态下读取全部批次。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>批次字典。</returns>
    private async Task<Dictionary<string, SyncBatch>> LoadAllWithoutLockAsync(CancellationToken ct)
    {
        if (!File.Exists(_batchFilePath))
        {
            return new Dictionary<string, SyncBatch>(StringComparer.OrdinalIgnoreCase);
        }

        var json = await File.ReadAllTextAsync(_batchFilePath, ct);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, SyncBatch>(StringComparer.OrdinalIgnoreCase);
        }

        var data = JsonSerializer.Deserialize<Dictionary<string, SyncBatch>>(json) ?? new Dictionary<string, SyncBatch>(StringComparer.OrdinalIgnoreCase);
        return new Dictionary<string, SyncBatch>(data, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 克隆批次对象。
    /// </summary>
    /// <param name="batch">源批次。</param>
    /// <returns>克隆结果。</returns>
    private static SyncBatch CloneBatch(SyncBatch batch)
    {
        return new SyncBatch
        {
            BatchId = batch.BatchId,
            ParentBatchId = batch.ParentBatchId,
            TableCode = batch.TableCode,
            WindowStartLocal = batch.WindowStartLocal,
            WindowEndLocal = batch.WindowEndLocal,
            ReadCount = batch.ReadCount,
            InsertCount = batch.InsertCount,
            UpdateCount = batch.UpdateCount,
            DeleteCount = batch.DeleteCount,
            SkipCount = batch.SkipCount,
            Status = batch.Status,
            StartedTimeLocal = batch.StartedTimeLocal,
            CompletedTimeLocal = batch.CompletedTimeLocal,
            ErrorMessage = batch.ErrorMessage,
        };
    }
}
