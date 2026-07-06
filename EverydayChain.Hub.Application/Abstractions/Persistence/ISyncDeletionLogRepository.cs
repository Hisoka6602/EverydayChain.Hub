using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface ISyncDeletionLogRepository
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task WriteDeletionsAsync(IReadOnlyList<SyncDeletionLog> logs, CancellationToken ct);
}

