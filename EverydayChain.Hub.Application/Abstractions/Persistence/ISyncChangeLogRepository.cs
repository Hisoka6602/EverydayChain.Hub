using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义 ISyncChangeLogRepository 类型。
/// </summary>
public interface ISyncChangeLogRepository
{
    /// <summary>
    /// 执行 WriteChangesAsync 方法。
    /// </summary>
    Task WriteChangesAsync(IReadOnlyList<SyncChangeLog> changes, CancellationToken ct);
}

