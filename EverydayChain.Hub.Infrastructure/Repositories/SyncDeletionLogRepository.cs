using System.Collections.Concurrent;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 同步删除日志仓储基础实现（内存存储）。
/// 队列上限为 <see cref="MaxQueueCapacity"/>，超限时自动淘汰最早入队的条目，防止长期运行导致 OOM。
/// 生产环境建议替换为持久化实现。
/// </summary>
public class SyncDeletionLogRepository : ISyncDeletionLogRepository
{
    /// <summary>内存队列条目上限。</summary>
    private const int MaxQueueCapacity = 200_000;

    /// <summary>触发淘汰时单次最多移除的条目数。</summary>
    private const int EvictionBatchSize = 10_000;

    /// <summary>删除日志集合。</summary>
    private readonly ConcurrentQueue<SyncDeletionLog> _logs = new();

    /// <inheritdoc/>
    public Task WriteDeletionsAsync(IReadOnlyList<SyncDeletionLog> logs, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var stagedLogs = new List<SyncDeletionLog>(logs.Count);
        foreach (var log in logs)
        {
            ct.ThrowIfCancellationRequested();
            stagedLogs.Add(CloneLog(log));
        }

        // 克隆阶段完成后，不再检查取消令牌，确保批次整体原子性入队。
        foreach (var log in stagedLogs)
        {
            _logs.Enqueue(log);
        }

        TrimExcessIfNeeded();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 当队列条目超过 <see cref="MaxQueueCapacity"/> 时，淘汰最早入队的条目，防止无界增长。
    /// </summary>
    private void TrimExcessIfNeeded()
    {
        if (_logs.Count <= MaxQueueCapacity)
        {
            return;
        }

        var evictionCount = _logs.Count - MaxQueueCapacity + EvictionBatchSize;
        for (var i = 0; i < evictionCount; i++)
        {
            _logs.TryDequeue(out _);
        }
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
