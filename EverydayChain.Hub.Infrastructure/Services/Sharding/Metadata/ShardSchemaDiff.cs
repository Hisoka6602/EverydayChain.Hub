namespace EverydayChain.Hub.Infrastructure.Services.Sharding.Metadata;

/// <summary>
/// 分表结构差异结果。
/// </summary>
/// <param name="MissingColumns">缺失列。</param>
/// <param name="MissingIndexes">缺失索引。</param>
/// <param name="Warnings">告警集合。</param>
public readonly record struct ShardSchemaDiff(
    IReadOnlyList<ShardColumnSchema> MissingColumns,
    IReadOnlyList<ShardIndexSchema> MissingIndexes,
    IReadOnlyList<string> Warnings)
{
    /// <summary>
    /// 是否存在可执行的结构补齐动作。
    /// </summary>
    public bool HasChanges => MissingColumns.Count > 0 || MissingIndexes.Count > 0;
}
