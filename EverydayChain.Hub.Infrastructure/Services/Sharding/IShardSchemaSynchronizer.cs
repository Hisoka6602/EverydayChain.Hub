namespace EverydayChain.Hub.Infrastructure.Services.Sharding;

/// <summary>
/// 分表结构同步抽象，负责将逻辑表最新结构扩散到历史分表。
/// </summary>
public interface IShardSchemaSynchronizer
{
    /// <summary>
    /// 同步全部纳管逻辑表的历史分表结构。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    Task SynchronizeAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 同步指定逻辑表的历史分表结构。
    /// </summary>
    /// <param name="logicalTable">逻辑表名。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task SynchronizeTableAsync(string logicalTable, CancellationToken cancellationToken);
}
