using System.Collections.Concurrent;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 同步变更日志仓储基础实现（内存存储）。
/// 队列上限为 <see cref="MaxQueueCapacity"/>，超限时自动淘汰最早入队的条目，防止长期运行导致 OOM。
/// 生产环境建议替换为持久化实现。
/// </summary>
public class SyncChangeLogRepository : ISyncChangeLogRepository
{
    /// <summary>内存队列条目上限。</summary>
    private const int MaxQueueCapacity = 200_000;

    /// <summary>触发淘汰时单次最多移除的条目数。</summary>
    private const int EvictionBatchSize = 10_000;

    /// <summary>变更日志集合。</summary>
    private readonly ConcurrentQueue<SyncChangeLog> _changes = new();

    /// <inheritdoc/>
    public Task WriteChangesAsync(IReadOnlyList<SyncChangeLog> changes, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var stagedChanges = new List<SyncChangeLog>(changes.Count);
        foreach (var change in changes)
        {
            ct.ThrowIfCancellationRequested();
            stagedChanges.Add(CloneChange(change));
        }

        // 克隆阶段完成后，不再检查取消令牌，确保批次整体原子性入队。
        foreach (var change in stagedChanges)
        {
            _changes.Enqueue(change);
        }

        TrimExcessIfNeeded();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 当队列条目超过 <see cref="MaxQueueCapacity"/> 时，淘汰最早入队的条目，防止无界增长。
    /// </summary>
    private void TrimExcessIfNeeded()
    {
        if (_changes.Count <= MaxQueueCapacity)
        {
            return;
        }

        var toRemove = _changes.Count - MaxQueueCapacity + EvictionBatchSize;
        for (var i = 0; i < toRemove; i++)
        {
            _changes.TryDequeue(out _);
        }
    }

    /// <summary>
    /// 克隆变更日志对象。
    /// </summary>
    /// <param name="change">源对象。</param>
    /// <returns>克隆结果。</returns>
    private static SyncChangeLog CloneChange(SyncChangeLog change)
    {
        return new SyncChangeLog
        {
            BatchId = change.BatchId,
            ParentBatchId = change.ParentBatchId,
            TableCode = change.TableCode,
            OperationType = change.OperationType,
            BusinessKey = change.BusinessKey,
            BeforeSnapshot = change.BeforeSnapshot,
            AfterSnapshot = change.AfterSnapshot,
            ChangedTimeLocal = change.ChangedTimeLocal,
        };
    }
}
