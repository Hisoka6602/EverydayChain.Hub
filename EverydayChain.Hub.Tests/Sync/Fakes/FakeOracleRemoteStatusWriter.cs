using EverydayChain.Hub.Application.Abstractions.Sync;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Domain.Sync.Models;

namespace EverydayChain.Hub.Tests.Sync.Fakes;

/// <summary>
/// 远端状态回写测试替身。
/// </summary>
public class FakeOracleRemoteStatusWriter : IOracleRemoteStatusWriter
{
    /// <summary>累计回写行数。</summary>
    public int TotalWriteBackRows { get; private set; }

    /// <summary>最近一次回写批次号。</summary>
    public string LastBatchId { get; private set; } = string.Empty;

    /// <inheritdoc/>
    public Task<int> WriteBackByRowIdAsync(
        SyncTableDefinition definition,
        RemoteStatusConsumeProfile profile,
        string batchId,
        IReadOnlyList<string> rowIds,
        CancellationToken ct)
    {
        LastBatchId = batchId;
        TotalWriteBackRows += rowIds.Count;
        return Task.FromResult(rowIds.Count);
    }
}
