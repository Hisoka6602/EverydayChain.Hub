namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义保留期治理仓储契约。
/// 该仓储同时负责旧分表删除与固定表旧数据行删除。
/// </summary>
public interface IShardRetentionRepository
{
    /// <summary>
    /// 为指定分表生成回滚脚本。
    /// </summary>
    /// <param name="logicalTableName">逻辑表名。</param>
    /// <param name="physicalTableName">物理分表名。</param>
    /// <param name="ct">取消令牌。</param>
    Task<string> GenerateRollbackScriptAsync(string logicalTableName, string physicalTableName, CancellationToken ct);

    /// <summary>
    /// 删除指定旧分表。
    /// </summary>
    /// <param name="logicalTableName">逻辑表名。</param>
    /// <param name="physicalTableName">物理分表名。</param>
    /// <param name="rollbackScript">回滚脚本。</param>
    /// <param name="ct">取消令牌。</param>
    Task DropShardTableAsync(string logicalTableName, string physicalTableName, string rollbackScript, CancellationToken ct);

    /// <summary>
    /// 统计固定表中早于阈值时间的旧数据行数量。
    /// </summary>
    /// <param name="tableName">固定表名。</param>
    /// <param name="timeColumnName">时间列名。</param>
    /// <param name="thresholdTimeLocal">本地时间阈值。</param>
    /// <param name="ct">取消令牌。</param>
    Task<int> CountRowsBeforeAsync(string tableName, string timeColumnName, DateTime thresholdTimeLocal, CancellationToken ct);

    /// <summary>
    /// 按时间列批量删除固定表中的旧数据行。
    /// </summary>
    /// <param name="tableName">固定表名。</param>
    /// <param name="timeColumnName">时间列名。</param>
    /// <param name="thresholdTimeLocal">本地时间阈值。</param>
    /// <param name="batchSize">单批删除行数。</param>
    /// <param name="ct">取消令牌。</param>
    Task<int> DeleteRowsBeforeAsync(string tableName, string timeColumnName, DateTime thresholdTimeLocal, int batchSize, CancellationToken ct);
}
