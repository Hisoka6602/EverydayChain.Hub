namespace EverydayChain.Hub.Application.Abstractions.Infrastructure;

/// <summary>
/// 定义 IShardSchemaSynchronizer 类型。
/// </summary>
public interface IShardSchemaSynchronizer
{
    /// <summary>
    /// 执行 SynchronizeAllAsync 方法。
    /// </summary>
    Task SynchronizeAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 执行 SynchronizeTableAsync 方法。
    /// </summary>
    Task SynchronizeTableAsync(string logicalTable, CancellationToken cancellationToken);
}

