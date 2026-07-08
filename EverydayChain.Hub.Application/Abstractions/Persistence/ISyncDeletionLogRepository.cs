using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义 ISyncDeletionLogRepository 类型。
/// </summary>
public interface ISyncDeletionLogRepository
{
    /// <summary>
    /// 执行 WriteDeletionsAsync 方法。
    /// </summary>
    Task WriteDeletionsAsync(IReadOnlyList<SyncDeletionLog> logs, CancellationToken ct);
}

