using System.Collections.Concurrent;
using EverydayChain.Hub.Application.Repositories;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 同步批次仓储基础实现（内存存储）。
/// </summary>
public class SyncBatchRepository : ISyncBatchRepository
{
    /// <summary>批次存储字典。</summary>
    private readonly ConcurrentDictionary<string, SyncBatch> _batches = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public Task CreateBatchAsync(SyncBatch batch, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(batch.BatchId))
        {
            throw new InvalidOperationException("BatchId 不能为空。");
        }

        batch.Status = SyncBatchStatus.Pending;
        if (!_batches.TryAdd(batch.BatchId, CloneBatch(batch)))
        {
            throw new InvalidOperationException($"批次已存在：{batch.BatchId}");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task MarkInProgressAsync(string batchId, DateTime startedTimeLocal, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var batch = GetRequiredBatch(batchId);
        batch.Status = SyncBatchStatus.InProgress;
        batch.StartedTimeLocal = startedTimeLocal;
        _batches[batchId] = batch;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task CompleteBatchAsync(SyncBatchResult result, DateTime completedTimeLocal, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var batch = GetRequiredBatch(result.BatchId);
        batch.Status = SyncBatchStatus.Completed;
        batch.CompletedTimeLocal = completedTimeLocal;
        batch.ReadCount = result.ReadCount;
        batch.InsertCount = result.InsertCount;
        batch.UpdateCount = result.UpdateCount;
        batch.DeleteCount = result.DeleteCount;
        batch.SkipCount = result.SkipCount;
        batch.ErrorMessage = null;
        _batches[result.BatchId] = batch;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task FailBatchAsync(string batchId, string errorMessage, DateTime failedTimeLocal, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var batch = GetRequiredBatch(batchId);
        batch.Status = SyncBatchStatus.Failed;
        batch.CompletedTimeLocal = failedTimeLocal;
        batch.ErrorMessage = errorMessage;
        _batches[batchId] = batch;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<string?> GetLatestFailedBatchIdAsync(string tableCode, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var latestFailedBatch = _batches.Values
            .Where(batch => string.Equals(batch.TableCode, tableCode, StringComparison.OrdinalIgnoreCase)
                && batch.Status == SyncBatchStatus.Failed)
            .OrderByDescending(batch => batch.CompletedTimeLocal ?? DateTime.MinValue)
            .FirstOrDefault();
        return Task.FromResult(latestFailedBatch?.BatchId);
    }

    /// <summary>
    /// 获取必须存在的批次。
    /// </summary>
    /// <param name="batchId">批次编号。</param>
    /// <returns>批次对象。</returns>
    private SyncBatch GetRequiredBatch(string batchId)
    {
        if (_batches.TryGetValue(batchId, out var batch))
        {
            return CloneBatch(batch);
        }

        throw new InvalidOperationException($"未找到批次：{batchId}");
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
