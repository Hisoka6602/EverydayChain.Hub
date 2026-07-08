using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义 ISyncOrchestrator 类型。
/// </summary>
public interface ISyncOrchestrator
{
    /// <summary>
    /// 执行 RunTableSyncAsync 方法。
    /// </summary>
    Task<SyncBatchResult> RunTableSyncAsync(string tableCode, CancellationToken ct);

    /// <summary>
    /// 执行 RunAllEnabledTableSyncAsync 方法。
    /// </summary>
    Task<IReadOnlyList<SyncBatchResult>> RunAllEnabledTableSyncAsync(CancellationToken ct);
}

