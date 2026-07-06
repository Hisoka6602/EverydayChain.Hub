namespace EverydayChain.Hub.Infrastructure.Services.Sharding.Metadata;

/// <summary>
/// 定义当前类型。
/// </summary>
public readonly record struct ShardSchemaDiff(
    IReadOnlyList<ShardColumnSchema> MissingColumns,
    IReadOnlyList<ShardIndexSchema> MissingIndexes,
    IReadOnlyList<string> Warnings)
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool HasChanges => MissingColumns.Count > 0 || MissingIndexes.Count > 0;
}


