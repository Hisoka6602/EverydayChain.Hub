namespace EverydayChain.Hub.Infrastructure.Services.Sharding.Metadata;

/// <summary>
/// 定义 ShardSchemaDiff 类型。
/// </summary>
public readonly record struct ShardSchemaDiff(
    IReadOnlyList<ShardColumnSchema> MissingColumns,
    IReadOnlyList<ShardIndexSchema> MissingIndexes,
    IReadOnlyList<string> Warnings)
{
    /// <summary>
    /// 获取或设置 HasChanges。
    /// </summary>
    public bool HasChanges => MissingColumns.Count > 0 || MissingIndexes.Count > 0;
}


