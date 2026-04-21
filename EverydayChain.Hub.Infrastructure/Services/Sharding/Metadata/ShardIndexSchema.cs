namespace EverydayChain.Hub.Infrastructure.Services.Sharding.Metadata;

/// <summary>
/// 分表索引结构元数据。
/// </summary>
/// <param name="DatabaseName">索引名。</param>
/// <param name="IsUnique">是否唯一。</param>
/// <param name="ColumnNames">索引列。</param>
public readonly record struct ShardIndexSchema(
    string DatabaseName,
    bool IsUnique,
    IReadOnlyList<string> ColumnNames);
