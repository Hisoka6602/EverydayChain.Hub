namespace EverydayChain.Hub.Application.Repositories;

/// <summary>
/// 分表保留期仓储接口。
/// </summary>
public interface IShardRetentionRepository
{
    /// <summary>
    /// 生成指定物理分表的完整回滚脚本（可直接回放 DDL）。
    /// </summary>
    /// <param name="logicalTableName">逻辑表名。</param>
    /// <param name="physicalTableName">物理表名。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>回滚脚本文本。</returns>
    Task<string> GenerateRollbackScriptAsync(string logicalTableName, string physicalTableName, CancellationToken ct);

    /// <summary>
    /// 删除指定物理分表。
    /// </summary>
    /// <param name="logicalTableName">逻辑表名。</param>
    /// <param name="physicalTableName">物理表名。</param>
    /// <param name="rollbackScript">回滚脚本。</param>
    /// <param name="ct">取消令牌。</param>
    Task DropShardTableAsync(string logicalTableName, string physicalTableName, string rollbackScript, CancellationToken ct);
}
