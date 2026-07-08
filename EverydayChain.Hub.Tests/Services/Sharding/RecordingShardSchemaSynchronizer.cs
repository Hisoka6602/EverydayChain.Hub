using EverydayChain.Hub.Application.Abstractions.Infrastructure;

namespace EverydayChain.Hub.Tests.Services.Sharding;

/// <summary>
/// 定义 RecordingShardSchemaSynchronizer 类型。
/// </summary>
public sealed class RecordingShardSchemaSynchronizer : IShardSchemaSynchronizer
{
    /// <summary>
    /// 获取或设置 SynchronizeAllCallCount。
    /// </summary>
    public int SynchronizeAllCallCount { get; private set; }

    /// <summary>
    /// 获取或设置 SynchronizedLogicalTables。
    /// </summary>
    public List<string> SynchronizedLogicalTables { get; } = [];

    public Task SynchronizeAllAsync(CancellationToken cancellationToken)
    {
        SynchronizeAllCallCount++;
        return Task.CompletedTask;
    }

    public Task SynchronizeTableAsync(string logicalTable, CancellationToken cancellationToken)
    {
        SynchronizedLogicalTables.Add(logicalTable);
        return Task.CompletedTask;
    }
}

