using System.Collections.Concurrent;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.SharedKernel.Utilities;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 同步删除日志仓储内存实现。
/// 队列上限为 <see cref="MaxQueueCapacity"/>，超限时自动淘汰最早入队的条目，防止长期运行导致 OOM。
/// 生产环境建议替换为持久化实现。
/// </summary>
public class InMemorySyncDeletionLogRepository : ISyncDeletionLogRepository
{
    /// <summary>内存队列条目上限（水位上限）。</summary>
    private const int MaxQueueCapacity = 200_000;

    /// <summary>
    /// 超限后额外多移除的条目数，使水位回落至上限以下，降低后续写入频繁触发淘汰的概率。
    /// 实际单次移除数为 <c>currentCount - MaxQueueCapacity + ExtraEvictionCount</c>，
    /// 而非固定 10,000。
    /// </summary>
    private const int ExtraEvictionCount = 10_000;

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

        BoundedConcurrentQueueHelper.TrimExcessIfNeeded(_logs, MaxQueueCapacity, ExtraEvictionCount);
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
