namespace EverydayChain.Hub.Infrastructure.Services.Sharding.Metadata;

/// <summary>
/// 分表列结构元数据。
/// </summary>
/// <param name="ColumnName">列名。</param>
/// <param name="StoreType">数据库类型。</param>
/// <param name="IsNullable">是否可空。</param>
/// <param name="IsIdentity">是否自增。</param>
/// <param name="DefaultValueSql">默认值 SQL 表达式。</param>
/// <param name="DefaultValue">默认值常量。</param>
/// <param name="Ordinal">列顺序。</param>
public readonly record struct ShardColumnSchema(
    string ColumnName,
    string StoreType,
    bool IsNullable,
    bool IsIdentity,
    string? DefaultValueSql,
    object? DefaultValue,
    int Ordinal);
