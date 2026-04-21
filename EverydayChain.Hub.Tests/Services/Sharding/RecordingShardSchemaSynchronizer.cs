using EverydayChain.Hub.Application.Abstractions.Infrastructure;

namespace EverydayChain.Hub.Tests.Services.Sharding;

/// <summary>
/// 记录分表结构同步调用的测试桩。
/// </summary>
public sealed class RecordingShardSchemaSynchronizer : IShardSchemaSynchronizer
{
    /// <summary>全量同步调用次数。</summary>
    public int SynchronizeAllCallCount { get; private set; }

    /// <summary>按表同步记录。</summary>
    public List<string> SynchronizedLogicalTables { get; } = [];

    /// <inheritdoc />
    public Task SynchronizeAllAsync(CancellationToken cancellationToken)
    {
        SynchronizeAllCallCount++;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SynchronizeTableAsync(string logicalTable, CancellationToken cancellationToken)
    {
        SynchronizedLogicalTables.Add(logicalTable);
        return Task.CompletedTask;
    }
}
