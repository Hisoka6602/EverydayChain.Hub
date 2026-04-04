using System.Collections.Concurrent;
using EverydayChain.Hub.Application.Repositories;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 同步变更日志仓储基础实现（内存存储）。
/// </summary>
public class SyncChangeLogRepository : ISyncChangeLogRepository
{
    /// <summary>变更日志集合。</summary>
    private readonly ConcurrentQueue<SyncChangeLog> _changes = new();

    /// <inheritdoc/>
    public Task WriteChangesAsync(IReadOnlyList<SyncChangeLog> changes, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        foreach (var change in changes)
        {
            _changes.Enqueue(CloneChange(change));
        }

        return Task.CompletedTask;
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
