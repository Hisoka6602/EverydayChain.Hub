namespace EverydayChain.Hub.Infrastructure.Services.Sharding.Metadata;

/// <summary>
/// 物理分表实际结构元数据。
/// </summary>
/// <param name="Schema">Schema 名称。</param>
/// <param name="TableName">物理表名。</param>
/// <param name="Columns">列集合。</param>
/// <param name="PrimaryKeyColumns">主键列集合。</param>
/// <param name="Indexes">索引集合。</param>
public readonly record struct ShardPhysicalTableSchema(
    string Schema,
    string TableName,
    IReadOnlyList<ShardColumnSchema> Columns,
    IReadOnlyList<string> PrimaryKeyColumns,
    IReadOnlyList<ShardIndexSchema> Indexes);
