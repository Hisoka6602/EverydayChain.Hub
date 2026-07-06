using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Domain.Sync.Models;

namespace EverydayChain.Hub.Application.Abstractions.Sync;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IOracleRemoteStatusWriter
{
    Task<int> WriteBackByRowIdAsync(
        SyncTableDefinition definition,
        RemoteStatusConsumeProfile profile,
        string batchId,
        IReadOnlyList<string> rowIds,
        CancellationToken ct);
}

