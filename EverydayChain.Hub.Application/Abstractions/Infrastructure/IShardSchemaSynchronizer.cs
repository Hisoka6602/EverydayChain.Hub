namespace EverydayChain.Hub.Application.Abstractions.Infrastructure;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IShardSchemaSynchronizer
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task SynchronizeAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task SynchronizeTableAsync(string logicalTable, CancellationToken cancellationToken);
}

