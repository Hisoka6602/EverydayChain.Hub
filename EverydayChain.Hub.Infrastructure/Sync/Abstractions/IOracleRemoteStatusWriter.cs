using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Domain.Sync.Models;

namespace EverydayChain.Hub.Infrastructure.Sync.Abstractions;

/// <summary>
/// Oracle 远端状态回写抽象。
/// </summary>
public interface IOracleRemoteStatusWriter
{
    /// <summary>
    /// 按 ROWID 批量回写远端状态。
    /// </summary>
    /// <param name="definition">同步表定义。</param>
    /// <param name="profile">状态消费配置。</param>
    /// <param name="rowIds">ROWID 集合。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>成功回写行数。</returns>
    Task<int> WriteBackByRowIdAsync(
        SyncTableDefinition definition,
        RemoteStatusConsumeProfile profile,
        IReadOnlyList<string> rowIds,
        CancellationToken ct);
}
