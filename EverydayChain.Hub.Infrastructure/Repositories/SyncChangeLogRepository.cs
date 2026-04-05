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
        // 单次遍历完成克隆与入队，避免双重循环引入额外中间列表分配。
        // ConcurrentQueue.Enqueue 每项操作为原子操作，暂存后二次入队不能提升批量事务性。
        foreach (var change in changes)
        {
            ct.ThrowIfCancellationRequested();
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
