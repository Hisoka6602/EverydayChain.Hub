using EverydayChain.Hub.SharedKernel.Utilities;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 同步编排服务接口。
/// </summary>
public interface ISyncOrchestrator
{
    /// <summary>
    /// 执行指定表同步。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>同步批次结果。</returns>
    Task<SyncBatchResult> RunTableSyncAsync(string tableCode, CancellationToken ct);

    /// <summary>
    /// 执行全部启用表同步。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>同步批次结果列表。</returns>
    Task<IReadOnlyList<SyncBatchResult>> RunAllEnabledTableSyncAsync(CancellationToken ct);
}
