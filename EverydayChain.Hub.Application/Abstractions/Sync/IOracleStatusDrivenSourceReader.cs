using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Domain.Sync.Models;

namespace EverydayChain.Hub.Application.Abstractions.Sync;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IOracleStatusDrivenSourceReader
{
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ReadPendingPageAsync(
        SyncTableDefinition definition,
        RemoteStatusConsumeProfile profile,
        int pageNo,
        int pageSize,
        IReadOnlySet<string> normalizedExcludedColumns,
        SyncWindow window,
        CancellationToken ct);
}

