using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface ISyncChangeLogRepository
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task WriteChangesAsync(IReadOnlyList<SyncChangeLog> changes, CancellationToken ct);
}

