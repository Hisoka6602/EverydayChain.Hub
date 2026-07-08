namespace EverydayChain.Hub.Infrastructure.Services.Sharding.Metadata;

/// <summary>
/// 定义 ShardTableSchemaTemplate 类型。
/// </summary>
public readonly record struct ShardTableSchemaTemplate(
    string LogicalTable,
    string Schema,
    IReadOnlyList<ShardColumnSchema> Columns,
    IReadOnlyList<string> PrimaryKeyColumns,
    IReadOnlyList<ShardIndexSchema> Indexes);


