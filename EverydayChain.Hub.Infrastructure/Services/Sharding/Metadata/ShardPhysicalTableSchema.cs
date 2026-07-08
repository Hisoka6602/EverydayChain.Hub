namespace EverydayChain.Hub.Infrastructure.Services.Sharding.Metadata;

/// <summary>
/// 定义 ShardPhysicalTableSchema 类型。
/// </summary>
public readonly record struct ShardPhysicalTableSchema(
    string Schema,
    string TableName,
    IReadOnlyList<ShardColumnSchema> Columns,
    IReadOnlyList<string> PrimaryKeyColumns,
    IReadOnlyList<ShardIndexSchema> Indexes);


