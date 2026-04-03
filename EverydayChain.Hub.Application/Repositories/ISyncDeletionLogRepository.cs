using EverydayChain.Hub.SharedKernel.Utilities;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Repositories;

/// <summary>
/// 同步删除日志仓储接口。
/// </summary>
public interface ISyncDeletionLogRepository
{
    /// <summary>
    /// 写入删除日志。
    /// </summary>
    /// <param name="logs">删除日志集合。</param>
    /// <param name="ct">取消令牌。</param>
    Task WriteDeletionsAsync(IReadOnlyList<SyncDeletionLog> logs, CancellationToken ct);
}
