namespace EverydayChain.Hub.Infrastructure.Services.Sharding.Metadata;

/// <summary>
/// 定义当前类型。
/// </summary>
public readonly record struct ShardIndexSchema(
    string DatabaseName,
    bool IsUnique,
    IReadOnlyList<string> ColumnNames);


