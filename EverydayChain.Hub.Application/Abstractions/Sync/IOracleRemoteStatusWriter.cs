using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Domain.Sync.Models;

namespace EverydayChain.Hub.Application.Abstractions.Sync;

/// <summary>
/// Oracle 远端状态回写抽象，表达向 Oracle 源端写回处理状态的外部协作能力。
/// </summary>
public interface IOracleRemoteStatusWriter
{
    /// <summary>
    /// 按 ROWID 批量回写远端状态。
    /// </summary>
    /// <param name="definition">同步表定义。</param>
    /// <param name="profile">状态消费配置。</param>
    /// <param name="batchId">当前同步批次号。</param>
    /// <param name="rowIds">ROWID 集合。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>成功回写行数。</returns>
    Task<int> WriteBackByRowIdAsync(
        SyncTableDefinition definition,
        RemoteStatusConsumeProfile profile,
        string batchId,
        IReadOnlyList<string> rowIds,
        CancellationToken ct);
}
