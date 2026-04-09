using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Domain.Sync.Models;
using EverydayChain.Hub.Infrastructure.Sync.Abstractions;

namespace EverydayChain.Hub.Tests.Sync.Fakes;

/// <summary>
/// 远端状态回写测试替身。
/// </summary>
public class FakeOracleRemoteStatusWriter : IOracleRemoteStatusWriter
{
    /// <summary>累计回写行数。</summary>
    public int TotalWriteBackRows { get; private set; }

    /// <inheritdoc/>
    public Task<int> WriteBackByRowIdAsync(
        SyncTableDefinition definition,
        RemoteStatusConsumeProfile profile,
        IReadOnlyList<string> rowIds,
        CancellationToken ct)
    {
        TotalWriteBackRows += rowIds.Count;
        return Task.FromResult(rowIds.Count);
    }
}
