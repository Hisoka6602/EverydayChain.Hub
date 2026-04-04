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
        foreach (var log in logs)
        {
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
