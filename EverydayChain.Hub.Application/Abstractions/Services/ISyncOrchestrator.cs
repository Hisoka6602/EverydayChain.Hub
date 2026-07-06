using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface ISyncOrchestrator
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<SyncBatchResult> RunTableSyncAsync(string tableCode, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<IReadOnlyList<SyncBatchResult>> RunAllEnabledTableSyncAsync(CancellationToken ct);
}

