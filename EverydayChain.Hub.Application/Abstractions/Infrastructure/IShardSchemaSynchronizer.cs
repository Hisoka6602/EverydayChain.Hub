namespace EverydayChain.Hub.Application.Abstractions.Infrastructure;

/// <summary>
/// 分表结构同步抽象，负责在主表迁移完成后将最新结构扩散到历史分表。
/// 该抽象仅自动补齐缺失可空列、缺失索引与带安全默认值的非空新增列；
/// 对于非空无默认值列、类型变更、危险可空性变更、删列、主键重建等破坏性升级只告警不强补。
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
