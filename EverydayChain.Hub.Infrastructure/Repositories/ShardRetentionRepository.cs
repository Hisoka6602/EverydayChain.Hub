using EverydayChain.Hub.Application.Repositories;
using EverydayChain.Hub.Infrastructure.Options;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.RegularExpressions;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 分表保留期仓储实现。
/// </summary>
public class ShardRetentionRepository(
    IOptions<ShardingOptions> shardingOptions,
    IDangerZoneExecutor dangerZoneExecutor,
    ILogger<ShardRetentionRepository> logger) : IShardRetentionRepository
{
    /// <summary>安全对象名校验正则（仅允许字母、数字、下划线）。</summary>
    private static readonly Regex SqlIdentifierRegex = new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);
    /// <summary>分表配置快照。</summary>
    private readonly ShardingOptions _options = shardingOptions.Value;

    /// <inheritdoc/>
    public Task<string> GenerateRollbackScriptAsync(string logicalTableName, string physicalTableName, CancellationToken ct)
    {
        if (!IsSafeSqlIdentifier(_options.Schema) || !IsSafeSqlIdentifier(physicalTableName))
        {
            throw new InvalidOperationException($"回滚脚本生成参数不合法。Schema={_options.Schema}, PhysicalTable={physicalTableName}");
        }

        return dangerZoneExecutor.ExecuteAsync($"rollback-script-{logicalTableName}-{physicalTableName}", async token =>
        {
            var columnsSql = """
SELECT
    c.column_id AS ColumnId,
    c.name AS ColumnName,
    t.name AS TypeName,
    c.max_length AS MaxLength,
    c.precision AS NumericPrecision,
    c.scale AS NumericScale,
    c.is_nullable AS IsNullable,
    c.is_identity AS IsIdentity
FROM sys.tables tb
INNER JOIN sys.schemas s ON s.schema_id = tb.schema_id
INNER JOIN sys.columns c ON c.object_id = tb.object_id
INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE s.name = @schemaName AND tb.name = @tableName
ORDER BY c.column_id;
""";

            var primaryKeySql = """
SELECT
    kc.name AS ConstraintName,
    c.name AS ColumnName,
    ic.is_descending_key AS IsDescending
FROM sys.tables tb
INNER JOIN sys.schemas s ON s.schema_id = tb.schema_id
INNER JOIN sys.key_constraints kc ON kc.parent_object_id = tb.object_id AND kc.type = 'PK'
INNER JOIN sys.index_columns ic ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE s.name = @schemaName AND tb.name = @tableName
ORDER BY ic.key_ordinal;
""";

            var indexesSql = """
SELECT
    i.name AS IndexName,
    i.is_unique AS IsUnique,
    i.type_desc AS TypeDesc,
    i.filter_definition AS FilterDefinition,
    i.fill_factor AS FillFactor,
    i.is_disabled AS IsDisabled,
    ic.key_ordinal AS KeyOrdinal,
    ic.is_included_column AS IsIncludedColumn,
    ic.is_descending_key AS IsDescending,
    c.name AS ColumnName
FROM sys.tables tb
INNER JOIN sys.schemas s ON s.schema_id = tb.schema_id
INNER JOIN sys.indexes i ON i.object_id = tb.object_id
INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE s.name = @schemaName AND tb.name = @tableName
  AND i.is_primary_key = 0
  AND i.is_hypothetical = 0
  AND i.name IS NOT NULL
ORDER BY i.name, ic.is_included_column, ic.key_ordinal, c.column_id;
""";

            var columns = new List<ColumnMetadata>();
            var pkColumns = new List<PrimaryKeyColumnMetadata>();
            var indexRows = new List<IndexMetadataRow>();
            await using var connection = new SqlConnection(_options.ConnectionString);
            await connection.OpenAsync(token);

            await using (var columnCommand = CreateMetadataCommand(connection, columnsSql, physicalTableName))
            await using (var columnReader = await columnCommand.ExecuteReaderAsync(token))
            {
                while (await columnReader.ReadAsync(token))
                {
                    columns.Add(new ColumnMetadata(
                        columnReader.GetInt32(0),
                        columnReader.GetString(1),
                        columnReader.GetString(2),
                        columnReader.GetInt16(3),
                        columnReader.GetByte(4),
                        columnReader.GetByte(5),
                        columnReader.GetBoolean(6),
                        columnReader.GetBoolean(7)));
                }
            }

            if (columns.Count == 0)
            {
                throw new InvalidOperationException($"未找到物理分表，无法生成回滚脚本。Schema={_options.Schema}, PhysicalTable={physicalTableName}");
            }

            await using (var pkCommand = CreateMetadataCommand(connection, primaryKeySql, physicalTableName))
            await using (var pkReader = await pkCommand.ExecuteReaderAsync(token))
            {
                while (await pkReader.ReadAsync(token))
                {
                    pkColumns.Add(new PrimaryKeyColumnMetadata(
                        pkReader.GetString(0),
                        pkReader.GetString(1),
                        pkReader.GetBoolean(2)));
                }
            }

            await using (var indexCommand = CreateMetadataCommand(connection, indexesSql, physicalTableName))
            await using (var indexReader = await indexCommand.ExecuteReaderAsync(token))
            {
                while (await indexReader.ReadAsync(token))
                {
                    indexRows.Add(new IndexMetadataRow(
                        indexReader.GetString(0),
                        indexReader.GetBoolean(1),
                        indexReader.GetString(2),
                        indexReader.IsDBNull(3) ? null : indexReader.GetString(3),
                        indexReader.GetByte(4),
                        indexReader.GetBoolean(5),
                        indexReader.GetInt32(6),
                        indexReader.GetBoolean(7),
                        indexReader.GetBoolean(8),
                        indexReader.GetString(9)));
                }
            }

            var scriptBuilder = new StringBuilder();
            scriptBuilder.AppendLine($"-- 回滚脚本：恢复分表 [{_options.Schema}].[{physicalTableName}]");
            scriptBuilder.AppendLine($"-- 逻辑表: {logicalTableName}");
            scriptBuilder.AppendLine($"-- 生成时间(本地): {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            scriptBuilder.AppendLine("IF OBJECT_ID(N'[" + _options.Schema + "].[" + physicalTableName + "]', N'U') IS NULL");
            scriptBuilder.AppendLine("BEGIN");
            scriptBuilder.AppendLine($"    CREATE TABLE [{_options.Schema}].[{physicalTableName}]");
            scriptBuilder.AppendLine("    (");
            for (var i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                var typeSql = BuildTypeSql(column);
                var identitySql = column.IsIdentity ? " IDENTITY(1,1)" : string.Empty;
                var nullabilitySql = column.IsNullable ? " NULL" : " NOT NULL";
                var suffix = i == columns.Count - 1 && pkColumns.Count == 0 ? string.Empty : ",";
                scriptBuilder.AppendLine($"        [{column.ColumnName}] {typeSql}{identitySql}{nullabilitySql}{suffix}");
            }

            if (pkColumns.Count > 0)
            {
                var pkName = pkColumns[0].ConstraintName;
                var pkColumnSql = string.Join(", ", pkColumns.Select(x => $"[{x.ColumnName}]{(x.IsDescending ? " DESC" : " ASC")}"));
                scriptBuilder.AppendLine($"        CONSTRAINT [{pkName}] PRIMARY KEY ({pkColumnSql})");
            }

            scriptBuilder.AppendLine("    );");

            var indexes = indexRows
                .GroupBy(x => new { x.IndexName, x.IsUnique, x.TypeDesc, x.FilterDefinition, x.FillFactor, x.IsDisabled })
                .OrderBy(x => x.Key.IndexName)
                .ToList();
            foreach (var index in indexes)
            {
                if (!string.Equals(index.Key.TypeDesc, "CLUSTERED", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(index.Key.TypeDesc, "NONCLUSTERED", StringComparison.OrdinalIgnoreCase))
                {
                    scriptBuilder.AppendLine($"    -- 跳过暂不支持的索引类型：[{index.Key.IndexName}] ({index.Key.TypeDesc})");
                    continue;
                }

                var uniqueSql = index.Key.IsUnique ? "UNIQUE " : string.Empty;
                var clusteredSql = string.Equals(index.Key.TypeDesc, "CLUSTERED", StringComparison.OrdinalIgnoreCase)
                    ? "CLUSTERED "
                    : "NONCLUSTERED ";
                var keyColumnsSqlPart = string.Join(", ", index
                    .Where(x => !x.IsIncludedColumn && x.KeyOrdinal > 0)
                    .OrderBy(x => x.KeyOrdinal)
                    .Select(x => $"[{x.ColumnName}]{(x.IsDescending ? " DESC" : " ASC")}"));
                if (string.IsNullOrWhiteSpace(keyColumnsSqlPart))
                {
                    scriptBuilder.AppendLine($"    -- 跳过无键列索引：[{index.Key.IndexName}]");
                    continue;
                }

                var includeColumns = index
                    .Where(x => x.IsIncludedColumn)
                    .Select(x => $"[{x.ColumnName}]")
                    .ToList();
                var includeSql = includeColumns.Count > 0
                    ? $" INCLUDE ({string.Join(", ", includeColumns)})"
                    : string.Empty;
                var filterSql = !string.IsNullOrWhiteSpace(index.Key.FilterDefinition)
                    ? $" WHERE {index.Key.FilterDefinition}"
                    : string.Empty;
                var fillFactorSql = index.Key.FillFactor > 0
                    ? $" WITH (FILLFACTOR = {index.Key.FillFactor})"
                    : string.Empty;
                scriptBuilder.AppendLine(
                    $"    CREATE {uniqueSql}{clusteredSql}INDEX [{index.Key.IndexName}] ON [{_options.Schema}].[{physicalTableName}] ({keyColumnsSqlPart}){includeSql}{filterSql}{fillFactorSql};");
                if (index.Key.IsDisabled)
                {
                    scriptBuilder.AppendLine($"    ALTER INDEX [{index.Key.IndexName}] ON [{_options.Schema}].[{physicalTableName}] DISABLE;");
                }
            }

            scriptBuilder.AppendLine("END");
            scriptBuilder.AppendLine("GO");
            return scriptBuilder.ToString();
        }, ct);
    }

    /// <inheritdoc/>
    public Task DropShardTableAsync(string logicalTableName, string physicalTableName, string rollbackScript, CancellationToken ct)
    {
        if (!IsSafeSqlIdentifier(_options.Schema) || !IsSafeSqlIdentifier(physicalTableName))
        {
            throw new InvalidOperationException($"分表删除参数不合法。Schema={_options.Schema}, PhysicalTable={physicalTableName}");
        }

        return dangerZoneExecutor.ExecuteAsync($"drop-shard-{logicalTableName}-{physicalTableName}", async token =>
        {
            var dropSql = $"DROP TABLE [{_options.Schema}].[{physicalTableName}];";
            await using var connection = new SqlConnection(_options.ConnectionString);
            await connection.OpenAsync(token);
            await using var command = connection.CreateCommand();
            command.CommandText = dropSql;
            await command.ExecuteNonQueryAsync(token);
            logger.LogWarning(
                "分表保留期已删除过期分表。LogicalTable={LogicalTable}, PhysicalTable={PhysicalTable}, RollbackScript={RollbackScript}",
                logicalTableName,
                physicalTableName,
                rollbackScript);
        }, ct);
    }

    /// <summary>
    /// 判断对象名是否满足安全标识符规则。
    /// </summary>
    /// <param name="identifier">对象名。</param>
    /// <returns>合法返回 <c>true</c>。</returns>
    private static bool IsSafeSqlIdentifier(string identifier)
    {
        return !string.IsNullOrWhiteSpace(identifier) && SqlIdentifierRegex.IsMatch(identifier);
    }

    /// <summary>
    /// 创建元数据查询命令。
    /// </summary>
    /// <param name="connection">数据库连接。</param>
    /// <param name="sql">查询 SQL。</param>
    /// <param name="tableName">表名。</param>
    /// <returns>配置好的命令对象。</returns>
    private SqlCommand CreateMetadataCommand(SqlConnection connection, string sql, string tableName)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@schemaName", _options.Schema));
        command.Parameters.Add(new SqlParameter("@tableName", tableName));
        return command;
    }

    /// <summary>
    /// 构建列类型 SQL 片段。
    /// </summary>
    /// <param name="column">列元数据。</param>
    /// <returns>类型 SQL。</returns>
    private static string BuildTypeSql(ColumnMetadata column)
    {
        var typeName = column.TypeName.ToLowerInvariant();
        return typeName switch
        {
            "nvarchar" or "nchar" => $"{column.TypeName}({GetUnicodeLengthSql(column.MaxLength)})",
            "varchar" or "char" or "varbinary" or "binary" => $"{column.TypeName}({GetLengthSql(column.MaxLength)})",
            "decimal" or "numeric" => $"{column.TypeName}({column.NumericPrecision},{column.NumericScale})",
            _ => column.TypeName
        };
    }

    /// <summary>
    /// 将字节长度转换为 Unicode 长度 SQL。
    /// </summary>
    /// <param name="maxLength">字节长度。</param>
    /// <returns>Unicode 长度表达式。</returns>
    private static string GetUnicodeLengthSql(short maxLength)
    {
        if (maxLength < 0)
        {
            return "MAX";
        }

        return (maxLength / 2).ToString();
    }

    /// <summary>
    /// 将字节长度转换为 SQL 长度。
    /// </summary>
    /// <param name="maxLength">字节长度。</param>
    /// <returns>长度表达式。</returns>
    private static string GetLengthSql(short maxLength)
    {
        return maxLength < 0 ? "MAX" : maxLength.ToString();
    }

    /// <summary>
    /// 列元数据模型。
    /// </summary>
    /// <param name="ColumnId">列序号。</param>
    /// <param name="ColumnName">列名。</param>
    /// <param name="TypeName">类型名。</param>
    /// <param name="MaxLength">长度。</param>
    /// <param name="NumericPrecision">精度。</param>
    /// <param name="NumericScale">小数位。</param>
    /// <param name="IsNullable">是否可空。</param>
    /// <param name="IsIdentity">是否自增。</param>
    private readonly record struct ColumnMetadata(
        int ColumnId,
        string ColumnName,
        string TypeName,
        short MaxLength,
        byte NumericPrecision,
        byte NumericScale,
        bool IsNullable,
        bool IsIdentity);

    /// <summary>
    /// 主键列元数据。
    /// </summary>
    /// <param name="ConstraintName">主键约束名。</param>
    /// <param name="ColumnName">列名。</param>
    /// <param name="IsDescending">是否倒序。</param>
    private readonly record struct PrimaryKeyColumnMetadata(
        string ConstraintName,
        string ColumnName,
        bool IsDescending);

    /// <summary>
    /// 索引元数据行。
    /// </summary>
    /// <param name="IndexName">索引名。</param>
    /// <param name="IsUnique">是否唯一。</param>
    /// <param name="TypeDesc">索引类型。</param>
    /// <param name="FilterDefinition">过滤条件。</param>
    /// <param name="FillFactor">填充因子。</param>
    /// <param name="IsDisabled">是否禁用。</param>
    /// <param name="KeyOrdinal">键序号。</param>
    /// <param name="IsIncludedColumn">是否包含列。</param>
    /// <param name="IsDescending">是否倒序。</param>
    /// <param name="ColumnName">列名。</param>
    private readonly record struct IndexMetadataRow(
        string IndexName,
        bool IsUnique,
        string TypeDesc,
        string? FilterDefinition,
        byte FillFactor,
        bool IsDisabled,
        int KeyOrdinal,
        bool IsIncludedColumn,
        bool IsDescending,
        string ColumnName);
}
