using EverydayChain.Hub.SharedKernel.Utilities;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Repositories;

/// <summary>
/// 同步变更日志仓储接口。
/// </summary>
public interface ISyncChangeLogRepository
{
    /// <summary>
    /// 写入变更日志。
    /// </summary>
    /// <param name="changes">变更集合。</param>
    /// <param name="ct">取消令牌。</param>
    Task WriteChangesAsync(IReadOnlyList<SyncChangeLog> changes, CancellationToken ct);
}
