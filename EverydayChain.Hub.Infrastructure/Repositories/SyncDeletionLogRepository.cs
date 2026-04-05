using System.Collections.Concurrent;
using EverydayChain.Hub.Application.Repositories;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 同步删除日志仓储基础实现（内存存储）。
/// </summary>
public class SyncDeletionLogRepository : ISyncDeletionLogRepository
{
    /// <summary>删除日志集合。</summary>
    private readonly ConcurrentQueue<SyncDeletionLog> _logs = new();

    /// <inheritdoc/>
    public Task WriteDeletionsAsync(IReadOnlyList<SyncDeletionLog> logs, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // 单次遍历完成克隆与入队，避免双重循环引入额外中间列表分配。
        // ConcurrentQueue.Enqueue 每项操作为原子操作，暂存后二次入队不能提升批量事务性。
        foreach (var log in logs)
        {
            ct.ThrowIfCancellationRequested();
            _logs.Enqueue(CloneLog(log));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 克隆删除日志。
    /// </summary>
    /// <param name="log">源日志。</param>
    /// <returns>克隆日志。</returns>
    private static SyncDeletionLog CloneLog(SyncDeletionLog log)
    {
        return new SyncDeletionLog
        {
            BatchId = log.BatchId,
            ParentBatchId = log.ParentBatchId,
            TableCode = log.TableCode,
            BusinessKey = log.BusinessKey,
            DeletionPolicy = log.DeletionPolicy,
            Executed = log.Executed,
            DeletedTimeLocal = log.DeletedTimeLocal,
            SourceEvidence = log.SourceEvidence,
        };
    }
}
