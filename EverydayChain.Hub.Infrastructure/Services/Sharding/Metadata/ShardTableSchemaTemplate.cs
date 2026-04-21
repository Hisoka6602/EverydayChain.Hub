namespace EverydayChain.Hub.Infrastructure.Services.Sharding.Metadata;

/// <summary>
/// 逻辑表目标结构模板。
/// </summary>
/// <param name="LogicalTable">逻辑表名。</param>
/// <param name="Schema">Schema 名称。</param>
/// <param name="Columns">列集合。</param>
/// <param name="PrimaryKeyColumns">主键列集合。</param>
/// <param name="Indexes">索引集合。</param>
public readonly record struct ShardTableSchemaTemplate(
    string LogicalTable,
    string Schema,
    IReadOnlyList<ShardColumnSchema> Columns,
    IReadOnlyList<string> PrimaryKeyColumns,
    IReadOnlyList<ShardIndexSchema> Indexes);
