namespace EverydayChain.Hub.Infrastructure.Services.Sharding.Metadata;

/// <summary>
/// 定义 ShardColumnSchema 类型。
/// </summary>
public readonly record struct ShardColumnSchema(
    string ColumnName,
    string StoreType,
    bool IsNullable,
    bool IsIdentity,
    string? DefaultValueSql,
    object? DefaultValue,
    int Ordinal);


