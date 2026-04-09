using System.Collections.Concurrent;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 同步批次仓储内存实现。
/// 内存条目上限为 <see cref="MaxBatchCount"/>，超限时自动淘汰最早完成/失败的批次，防止长期运行导致 OOM。
/// </summary>
public class InMemorySyncBatchRepository : ISyncBatchRepository
{
    /// <summary>内存批次条目上限。</summary>
    private const int MaxBatchCount = 5_000;

    /// <summary>触发淘汰时单次最多移除的条目数。</summary>
    private const int EvictionCount = 200;

    /// <summary>批次存储字典。</summary>
    private readonly ConcurrentDictionary<string, SyncBatch> _batches = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>各表最近失败批次索引。</summary>
    private readonly ConcurrentDictionary<string, (string BatchId, DateTime CompletedTimeLocal)> _latestFailedBatchIndex = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public Task CreateBatchAsync(SyncBatch batch, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(batch.BatchId))
        {
            throw new InvalidOperationException("BatchId 不能为空。");
        }

        if (string.IsNullOrWhiteSpace(batch.TableCode))
        {
            throw new InvalidOperationException("TableCode 不能为空。");
        }

        batch.Status = SyncBatchStatus.Pending;
        if (!_batches.TryAdd(batch.BatchId, CloneBatch(batch)))
        {
            throw new InvalidOperationException($"批次已存在：{batch.BatchId}");
        }

        TrimExcessBatchesIfNeeded();
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
        _latestFailedBatchIndex.AddOrUpdate(
            batch.TableCode,
            _ => (batch.BatchId, failedTimeLocal),
            (_, current) => current.CompletedTimeLocal >= failedTimeLocal
                ? current
                : (batch.BatchId, failedTimeLocal));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<string?> GetLatestFailedBatchIdAsync(string tableCode, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (_latestFailedBatchIndex.TryGetValue(tableCode, out var latestFailedBatch))
        {
            return Task.FromResult<string?>(latestFailedBatch.BatchId);
        }

        return Task.FromResult<string?>(null);
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
    /// 当内存批次数超过 <see cref="MaxBatchCount"/> 时，淘汰最早完成或失败的批次，防止无界增长。
    /// <para>
    /// <b>注意：</b>此方法仅淘汰 <c>Completed</c> 或 <c>Failed</c> 状态的批次。
    /// 若当前所有批次均处于 <c>Pending</c> 或 <c>InProgress</c>（正在运行中），
    /// 本次淘汰将无效，<see cref="MaxBatchCount"/> 为软上限，不强制拒绝新批次的创建。
    /// 正常生产场景下批次会迅速完成/失败并进入可淘汰状态，长期全部活跃批次超过上限的情况不应出现。
    /// </para>
    /// </summary>
    private void TrimExcessBatchesIfNeeded()
    {
        if (_batches.Count <= MaxBatchCount)
        {
            return;
        }

        var trimCandidates = _batches.Values
            .Where(static b => b.Status is SyncBatchStatus.Completed or SyncBatchStatus.Failed)
            .OrderBy(static b => b.CompletedTimeLocal ?? DateTime.MinValue)
            .Take(EvictionCount)
            .Select(static b => b.BatchId)
            .ToList();

        foreach (var id in trimCandidates)
        {
            _batches.TryRemove(id, out _);
        }
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
