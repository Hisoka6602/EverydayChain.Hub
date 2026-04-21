using System.Data;
using System.Globalization;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Services;
using EverydayChain.Hub.Infrastructure.Services.Sharding.Metadata;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Services.Sharding;

/// <summary>
/// 历史分表结构同步器，负责将 EF 当前模型扩散到已存在分表。
/// </summary>
public class ShardSchemaSynchronizer(
    IOptions<ShardingOptions> options,
    IReadOnlyList<string> managedLogicalTables,
    IDbContextFactory<HubDbContext> dbContextFactory,
    IShardTableResolver shardTableResolver,
    IDangerZoneExecutor dangerZoneExecutor,
    ILogger<ShardSchemaSynchronizer> logger) : IShardSchemaSynchronizer
{
    /// <summary>分表结构元数据查询超时秒数。</summary>
    private const int SchemaCommandTimeoutSeconds = 30;

    /// <summary>分表结构同步 DDL 超时秒数。</summary>
    private const int SynchronizeCommandTimeoutSeconds = 30;

    /// <summary>分表配置快照。</summary>
    private readonly ShardingOptions _options = options.Value;

    /// <summary>纳管逻辑表列表。</summary>
    private readonly IReadOnlyList<string> _managedLogicalTables = ShardSchemaTemplateBuilder.ValidateManagedLogicalTables(managedLogicalTables);

    /// <summary>逻辑表模板缓存。</summary>
    private readonly IReadOnlyDictionary<string, ShardTableSchemaTemplate> _tableTemplates = ShardSchemaTemplateBuilder.BuildTableTemplates(dbContextFactory, managedLogicalTables);

    /// <inheritdoc/>
    public async Task SynchronizeAllAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("分表结构同步开始：准备处理 {LogicalTableCount} 个纳管逻辑表。", _managedLogicalTables.Count);
        foreach (var logicalTable in _managedLogicalTables)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await SynchronizeTableAsync(logicalTable, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "分表结构同步失败但已隔离：逻辑表 {LogicalTable} 的全量同步未完成。", logicalTable);
            }
        }

        logger.LogInformation("分表结构同步完成：全部纳管逻辑表已处理完毕。LogicalTableCount={LogicalTableCount}", _managedLogicalTables.Count);
    }

    /// <inheritdoc/>
    public async Task SynchronizeTableAsync(string logicalTable, CancellationToken cancellationToken)
    {
        ValidateIdentifier(logicalTable, nameof(logicalTable));
        var template = ResolveTableTemplate(logicalTable);
        logger.LogInformation("分表结构同步开始：逻辑表 {LogicalTable} 开始检查历史分表结构。", logicalTable);

        var physicalTables = await shardTableResolver.ListPhysicalTablesAsync(logicalTable, cancellationToken);
        logger.LogInformation("分表结构同步发现历史分表：逻辑表 {LogicalTable} 共发现 {PhysicalTableCount} 张历史分表。", logicalTable, physicalTables.Count);
        foreach (var physicalTableName in physicalTables)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var physicalSchema = await ReadPhysicalTableSchemaAsync(template.Schema, physicalTableName, cancellationToken);
                var diff = BuildDiff(template, physicalSchema);
                LogDiff(logicalTable, template.Schema, physicalTableName, diff);
                if (!diff.HasChanges)
                {
                    logger.LogInformation("分表结构同步跳过：分表 {Schema}.{PhysicalTable} 与目标结构一致。", template.Schema, physicalTableName);
                    continue;
                }

                var sql = BuildSynchronizationSql(physicalTableName, template, diff);
                if (string.IsNullOrWhiteSpace(sql))
                {
                    logger.LogWarning("分表结构同步无可执行 DDL：分表 {Schema}.{PhysicalTable} 仅存在告警，未执行结构变更。", template.Schema, physicalTableName);
                    continue;
                }

                await ExecuteSynchronizationSqlAsync(logicalTable, physicalTableName, sql, cancellationToken);
                if (diff.MissingColumns.Count > 0)
                {
                    logger.LogInformation(
                        "分表结构同步已补齐缺列：分表 {Schema}.{PhysicalTable} 新增列 {Columns}。",
                        template.Schema,
                        physicalTableName,
                        string.Join(", ", diff.MissingColumns.Select(column => column.ColumnName)));
                }

                if (diff.MissingIndexes.Count > 0)
                {
                    var indexNames = diff.MissingIndexes
                        .Select(index => ShardSchemaTemplateBuilder.BuildPhysicalIndexName(template.LogicalTable, physicalTableName, index.DatabaseName))
                        .ToList();
                    logger.LogInformation(
                        "分表结构同步已补齐缺索引：分表 {Schema}.{PhysicalTable} 新建索引 {Indexes}。",
                        template.Schema,
                        physicalTableName,
                        string.Join(", ", indexNames));
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "分表结构同步失败但已隔离：逻辑表 {LogicalTable} 的分表 {Schema}.{PhysicalTable} 未同步成功。", logicalTable, template.Schema, physicalTableName);
            }
        }

        logger.LogInformation("分表结构同步结束：逻辑表 {LogicalTable} 的历史分表检查完成。", logicalTable);
    }

    /// <summary>
    /// 解析逻辑表对应的结构模板。
    /// </summary>
    /// <param name="logicalTable">逻辑表名。</param>
    /// <returns>结构模板。</returns>
    internal ShardTableSchemaTemplate ResolveTableTemplate(string logicalTable)
    {
        if (_tableTemplates.TryGetValue(logicalTable, out var template))
        {
            return template;
        }

        throw new InvalidOperationException($"分表结构同步配置无效：逻辑表 '{logicalTable}' 未配置结构模板。");
    }

    /// <summary>
    /// 计算逻辑表模板与物理分表之间的结构差异。
    /// </summary>
    /// <param name="template">逻辑表模板。</param>
    /// <param name="physicalSchema">物理分表结构。</param>
    /// <returns>差异结果。</returns>
    internal ShardSchemaDiff BuildDiff(ShardTableSchemaTemplate template, ShardPhysicalTableSchema physicalSchema)
    {
        var missingColumns = new List<ShardColumnSchema>();
        var missingIndexes = new List<ShardIndexSchema>();
        var warnings = new List<string>();
        var physicalColumns = physicalSchema.Columns.ToDictionary(column => column.ColumnName, StringComparer.OrdinalIgnoreCase);

        foreach (var expectedColumn in template.Columns)
        {
            if (!physicalColumns.TryGetValue(expectedColumn.ColumnName, out var actualColumn))
            {
                if (!expectedColumn.IsNullable && !CanAddSafely(expectedColumn))
                {
                    warnings.Add($"列 {expectedColumn.ColumnName} 为非空且缺少安全默认值，本次跳过自动补齐。");
                    continue;
                }

                missingColumns.Add(expectedColumn);
                continue;
            }

            if (!string.Equals(expectedColumn.StoreType, actualColumn.StoreType, StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"列 {expectedColumn.ColumnName} 类型不一致：目标={expectedColumn.StoreType}，实际={actualColumn.StoreType}。");
            }

            if (expectedColumn.IsNullable != actualColumn.IsNullable)
            {
                warnings.Add($"列 {expectedColumn.ColumnName} 可空性不一致：目标={(expectedColumn.IsNullable ? "NULL" : "NOT NULL")}，实际={(actualColumn.IsNullable ? "NULL" : "NOT NULL")}。");
            }
        }

        foreach (var expectedIndex in template.Indexes)
        {
            var expectedPhysicalIndexName = ShardSchemaTemplateBuilder.BuildPhysicalIndexName(template.LogicalTable, physicalSchema.TableName, expectedIndex.DatabaseName);
            var hasExactIndex = physicalSchema.Indexes.Any(index =>
                string.Equals(index.DatabaseName, expectedPhysicalIndexName, StringComparison.OrdinalIgnoreCase)
                || IsEquivalentIndex(index, expectedIndex));
            if (!hasExactIndex)
            {
                missingIndexes.Add(expectedIndex);
            }
        }

        if (template.PrimaryKeyColumns.Count > 0
            && !template.PrimaryKeyColumns.SequenceEqual(physicalSchema.PrimaryKeyColumns, StringComparer.OrdinalIgnoreCase))
        {
            warnings.Add($"主键定义不一致：目标主键列为 {string.Join(", ", template.PrimaryKeyColumns)}，实际主键列为 {string.Join(", ", physicalSchema.PrimaryKeyColumns)}。");
        }

        return new ShardSchemaDiff(missingColumns, missingIndexes, warnings);
    }

    /// <summary>
    /// 生成分表结构同步 SQL。
    /// </summary>
    /// <param name="physicalTableName">物理表名。</param>
    /// <param name="template">逻辑表模板。</param>
    /// <param name="diff">差异结果。</param>
    /// <returns>幂等 DDL。</returns>
    internal string BuildSynchronizationSql(string physicalTableName, ShardTableSchemaTemplate template, ShardSchemaDiff diff)
    {
        ValidateIdentifier(template.Schema, nameof(template.Schema));
        ValidateIdentifier(physicalTableName, nameof(physicalTableName));
        var sqlStatements = new List<string>();

        foreach (var column in diff.MissingColumns.OrderBy(item => item.Ordinal))
        {
            sqlStatements.Add(BuildAddColumnSql(template.Schema, physicalTableName, column));
        }

        foreach (var index in diff.MissingIndexes)
        {
            sqlStatements.Add(BuildCreateIndexSql(template, physicalTableName, index));
        }

        return string.Join(Environment.NewLine + Environment.NewLine, sqlStatements);
    }

    /// <summary>
    /// 读取物理分表结构。
    /// </summary>
    /// <param name="schema">Schema 名称。</param>
    /// <param name="physicalTableName">物理表名。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>物理分表结构。</returns>
    private async Task<ShardPhysicalTableSchema> ReadPhysicalTableSchemaAsync(string schema, string physicalTableName, CancellationToken cancellationToken)
    {
        ValidateIdentifier(schema, nameof(schema));
        ValidateIdentifier(physicalTableName, nameof(physicalTableName));

        const string columnsSql = """
            SELECT
                c.column_id AS ColumnId,
                c.name AS ColumnName,
                t.name AS TypeName,
                c.max_length AS MaxLength,
                c.precision AS NumericPrecision,
                c.scale AS NumericScale,
                c.is_nullable AS IsNullable,
                c.is_identity AS IsIdentity
            FROM sys.tables AS tb
            INNER JOIN sys.schemas AS s ON s.schema_id = tb.schema_id
            INNER JOIN sys.columns AS c ON c.object_id = tb.object_id
            INNER JOIN sys.types AS t ON t.user_type_id = c.user_type_id
            WHERE s.name = @schemaName AND tb.name = @tableName
            ORDER BY c.column_id;
            """;
        const string primaryKeySql = """
            SELECT c.name AS ColumnName
            FROM sys.tables AS tb
            INNER JOIN sys.schemas AS s ON s.schema_id = tb.schema_id
            INNER JOIN sys.key_constraints AS kc ON kc.parent_object_id = tb.object_id AND kc.type = 'PK'
            INNER JOIN sys.index_columns AS ic ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
            INNER JOIN sys.columns AS c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE s.name = @schemaName AND tb.name = @tableName
            ORDER BY ic.key_ordinal;
            """;
        const string indexesSql = """
            SELECT
                i.name AS IndexName,
                i.is_unique AS IsUnique,
                ic.key_ordinal AS KeyOrdinal,
                c.name AS ColumnName
            FROM sys.tables AS tb
            INNER JOIN sys.schemas AS s ON s.schema_id = tb.schema_id
            INNER JOIN sys.indexes AS i ON i.object_id = tb.object_id
            INNER JOIN sys.index_columns AS ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            INNER JOIN sys.columns AS c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE s.name = @schemaName AND tb.name = @tableName
              AND i.is_primary_key = 0
              AND i.is_hypothetical = 0
              AND i.name IS NOT NULL
              AND ic.is_included_column = 0
              AND ic.key_ordinal > 0
            ORDER BY i.name, ic.key_ordinal;
            """;

        var columns = new List<ShardColumnSchema>();
        var primaryKeyColumns = new List<string>();
        var indexRows = new List<PhysicalIndexRow>();
        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var columnCommand = CreateMetadataCommand(connection, columnsSql, schema, physicalTableName);
        await using (var columnReader = await columnCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await columnReader.ReadAsync(cancellationToken))
            {
                columns.Add(new ShardColumnSchema(
                    columnReader.GetString(1),
                    BuildStoreTypeSql(
                        columnReader.GetString(2),
                        columnReader.GetInt16(3),
                        columnReader.GetByte(4),
                        columnReader.GetByte(5)),
                    columnReader.GetBoolean(6),
                    columnReader.GetBoolean(7),
                    null,
                    null,
                    columnReader.GetInt32(0)));
            }
        }

        if (columns.Count == 0)
        {
            throw new InvalidOperationException($"未找到物理分表 {schema}.{physicalTableName}，无法执行结构同步。请检查该分表是否已被删除或当前 Schema 配置是否正确。");
        }

        await using var primaryKeyCommand = CreateMetadataCommand(connection, primaryKeySql, schema, physicalTableName);
        await using (var primaryKeyReader = await primaryKeyCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await primaryKeyReader.ReadAsync(cancellationToken))
            {
                primaryKeyColumns.Add(primaryKeyReader.GetString(0));
            }
        }

        await using var indexCommand = CreateMetadataCommand(connection, indexesSql, schema, physicalTableName);
        await using (var indexReader = await indexCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await indexReader.ReadAsync(cancellationToken))
            {
                indexRows.Add(new PhysicalIndexRow(
                    indexReader.GetString(0),
                    indexReader.GetBoolean(1),
                    indexReader.GetInt32(2),
                    indexReader.GetString(3)));
            }
        }

        var indexes = indexRows
            .GroupBy(row => new { row.IndexName, row.IsUnique })
            .Select(group => new ShardIndexSchema(
                group.Key.IndexName,
                group.Key.IsUnique,
                group.OrderBy(item => item.KeyOrdinal).Select(item => item.ColumnName).ToList()))
            .ToList();

        return new ShardPhysicalTableSchema(schema, physicalTableName, columns, primaryKeyColumns, indexes);
    }

    /// <summary>
    /// 输出结构差异日志。
    /// </summary>
    /// <param name="logicalTable">逻辑表名。</param>
    /// <param name="schema">Schema 名称。</param>
    /// <param name="physicalTableName">物理表名。</param>
    /// <param name="diff">差异结果。</param>
    private void LogDiff(string logicalTable, string schema, string physicalTableName, ShardSchemaDiff diff)
    {
        if (diff.MissingColumns.Count > 0)
        {
            logger.LogWarning(
                "分表结构同步发现缺列：逻辑表 {LogicalTable} 的分表 {Schema}.{PhysicalTable} 缺少列 {Columns}。",
                logicalTable,
                schema,
                physicalTableName,
                string.Join(", ", diff.MissingColumns.Select(column => column.ColumnName)));
        }

        if (diff.MissingIndexes.Count > 0)
        {
            logger.LogWarning(
                "分表结构同步发现缺索引：逻辑表 {LogicalTable} 的分表 {Schema}.{PhysicalTable} 缺少索引 {Indexes}。",
                logicalTable,
                schema,
                physicalTableName,
                string.Join(", ", diff.MissingIndexes.Select(index => index.DatabaseName)));
        }

        foreach (var warning in diff.Warnings)
        {
            logger.LogWarning("分表结构同步告警：逻辑表 {LogicalTable} 的分表 {Schema}.{PhysicalTable} 存在结构差异。{Warning}", logicalTable, schema, physicalTableName, warning);
        }
    }

    /// <summary>
    /// 执行结构同步 SQL。
    /// </summary>
    /// <param name="logicalTable">逻辑表名。</param>
    /// <param name="physicalTableName">物理表名。</param>
    /// <param name="sql">待执行 SQL。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    private Task ExecuteSynchronizationSqlAsync(string logicalTable, string physicalTableName, string sql, CancellationToken cancellationToken)
    {
        return dangerZoneExecutor.ExecuteAsync($"synchronize-shard-schema-{logicalTable}-{physicalTableName}", async token =>
        {
            await using var connection = new SqlConnection(_options.ConnectionString);
            await connection.OpenAsync(token);
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = SynchronizeCommandTimeoutSeconds;
            await command.ExecuteNonQueryAsync(token);
        }, cancellationToken, SynchronizeCommandTimeoutSeconds);
    }

    /// <summary>
    /// 构建缺列补齐 SQL。
    /// </summary>
    /// <param name="schema">Schema 名称。</param>
    /// <param name="physicalTableName">物理表名。</param>
    /// <param name="column">列模板。</param>
    /// <returns>幂等 SQL。</returns>
    private static string BuildAddColumnSql(string schema, string physicalTableName, ShardColumnSchema column)
    {
        var qualifiedTableLiteral = $"[{schema}].[{physicalTableName}]";
        var qualifiedTableNameLiteral = EscapeSqlLiteral(qualifiedTableLiteral);
        var columnNameLiteral = EscapeSqlLiteral(column.ColumnName);
        var definition = BuildAddColumnDefinition(column, physicalTableName);
        return $"""
            IF COL_LENGTH(N'{qualifiedTableNameLiteral}', N'{columnNameLiteral}') IS NULL
            BEGIN
                ALTER TABLE {qualifiedTableLiteral} ADD {definition};
            END;
            """;
    }

    /// <summary>
    /// 构建缺索引补齐 SQL。
    /// </summary>
    /// <param name="template">逻辑表模板。</param>
    /// <param name="physicalTableName">物理表名。</param>
    /// <param name="index">索引模板。</param>
    /// <returns>幂等 SQL。</returns>
    private static string BuildCreateIndexSql(ShardTableSchemaTemplate template, string physicalTableName, ShardIndexSchema index)
    {
        var physicalIndexName = ShardSchemaTemplateBuilder.BuildPhysicalIndexName(template.LogicalTable, physicalTableName, index.DatabaseName);
        var uniqueSql = index.IsUnique ? "UNIQUE " : string.Empty;
        var columnsSql = string.Join(", ", index.ColumnNames.Select(QuoteIdentifier));
        var qualifiedTable = $"{QuoteIdentifier(template.Schema)}.{QuoteIdentifier(physicalTableName)}";
        var schemaLiteral = EscapeSqlLiteral(template.Schema);
        var tableLiteral = EscapeSqlLiteral(physicalTableName);
        var indexLiteral = EscapeSqlLiteral(physicalIndexName);
        return $"""
            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes AS i
                INNER JOIN sys.tables AS t ON t.object_id = i.object_id
                INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
                WHERE s.name = N'{schemaLiteral}'
                  AND t.name = N'{tableLiteral}'
                  AND i.name = N'{indexLiteral}')
            BEGIN
                CREATE {uniqueSql}INDEX {QuoteIdentifier(physicalIndexName)} ON {qualifiedTable} ({columnsSql});
            END;
            """;
    }

    /// <summary>
    /// 构建新增列定义 SQL。
    /// </summary>
    /// <param name="column">列模板。</param>
    /// <param name="physicalTableName">物理表名。</param>
    /// <returns>列定义 SQL。</returns>
    private static string BuildAddColumnDefinition(ShardColumnSchema column, string physicalTableName)
    {
        var definition = $"{QuoteIdentifier(column.ColumnName)} {column.StoreType}";
        if (column.IsIdentity)
        {
            definition += " IDENTITY(1,1)";
        }

        if (column.IsNullable)
        {
            return definition + " NULL";
        }

        var defaultClause = BuildDefaultClause(column, physicalTableName);
        if (string.IsNullOrWhiteSpace(defaultClause))
        {
            throw new InvalidOperationException($"列 {column.ColumnName} 为非空列且未配置安全默认值，无法自动补齐。该列将被跳过，请先补充安全默认值或改为可空后重新同步。");
        }

        return definition + $" NOT NULL {defaultClause} WITH VALUES";
    }

    /// <summary>
    /// 构建默认值约束 SQL 片段。
    /// </summary>
    /// <param name="column">列模板。</param>
    /// <param name="physicalTableName">物理表名。</param>
    /// <returns>默认值约束 SQL。</returns>
    private static string BuildDefaultClause(ShardColumnSchema column, string physicalTableName)
    {
        if (!string.IsNullOrWhiteSpace(column.DefaultValueSql))
        {
            var constraintName = BuildDefaultConstraintName(physicalTableName, column.ColumnName);
            return $"CONSTRAINT {QuoteIdentifier(constraintName)} DEFAULT ({column.DefaultValueSql})";
        }

        if (column.DefaultValue is not null)
        {
            var constraintName = BuildDefaultConstraintName(physicalTableName, column.ColumnName);
            return $"CONSTRAINT {QuoteIdentifier(constraintName)} DEFAULT ({FormatDefaultValueLiteral(column.DefaultValue)})";
        }

        return string.Empty;
    }

    /// <summary>
    /// 判断缺失列是否可安全补齐。
    /// </summary>
    /// <param name="column">列模板。</param>
    /// <returns>可安全补齐返回 true。</returns>
    private static bool CanAddSafely(ShardColumnSchema column)
    {
        return !string.IsNullOrWhiteSpace(column.DefaultValueSql) || column.DefaultValue is not null;
    }

    /// <summary>
    /// 判断索引定义是否等价。
    /// </summary>
    /// <param name="actualIndex">实际索引。</param>
    /// <param name="expectedIndex">目标索引。</param>
    /// <returns>等价返回 true。</returns>
    private static bool IsEquivalentIndex(ShardIndexSchema actualIndex, ShardIndexSchema expectedIndex)
    {
        return actualIndex.IsUnique == expectedIndex.IsUnique
            && actualIndex.ColumnNames.SequenceEqual(expectedIndex.ColumnNames, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 创建结构元数据查询命令。
    /// </summary>
    /// <param name="connection">数据库连接。</param>
    /// <param name="sql">查询 SQL。</param>
    /// <param name="schema">Schema 名称。</param>
    /// <param name="tableName">表名。</param>
    /// <returns>配置好的命令对象。</returns>
    private static SqlCommand CreateMetadataCommand(SqlConnection connection, string sql, string schema, string tableName)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = SchemaCommandTimeoutSeconds;
        command.Parameters.Add(new SqlParameter("@schemaName", SqlDbType.NVarChar, 128) { Value = schema });
        command.Parameters.Add(new SqlParameter("@tableName", SqlDbType.NVarChar, 128) { Value = tableName });
        return command;
    }

    /// <summary>
    /// 根据系统表元数据重建列类型 SQL。
    /// </summary>
    /// <param name="typeName">类型名。</param>
    /// <param name="maxLength">最大长度。</param>
    /// <param name="numericPrecision">精度。</param>
    /// <param name="numericScale">小数位。</param>
    /// <returns>类型 SQL。</returns>
    private static string BuildStoreTypeSql(string typeName, short maxLength, byte numericPrecision, byte numericScale)
    {
        var normalizedTypeName = typeName.ToLowerInvariant();
        return normalizedTypeName switch
        {
            "nvarchar" or "nchar" => $"{typeName}({GetUnicodeLengthSql(maxLength)})",
            "varchar" or "char" or "varbinary" or "binary" => $"{typeName}({GetLengthSql(maxLength)})",
            "decimal" or "numeric" => $"{typeName}({numericPrecision},{numericScale})",
            _ => typeName
        };
    }

    /// <summary>
    /// 生成默认约束名。
    /// </summary>
    /// <param name="physicalTableName">物理表名。</param>
    /// <param name="columnName">列名。</param>
    /// <returns>约束名。</returns>
    private static string BuildDefaultConstraintName(string physicalTableName, string columnName)
    {
        var candidate = $"DF_{physicalTableName}_{columnName}";
        return candidate.Length <= 128 ? candidate : candidate[..128];
    }

    /// <summary>
    /// 将默认值对象格式化为 SQL 文本。
    /// </summary>
    /// <param name="value">默认值对象。</param>
    /// <returns>SQL 字面量。</returns>
    private static string FormatDefaultValueLiteral(object value)
    {
        return value switch
        {
            string text => $"N'{text.Replace("'", "''", StringComparison.Ordinal)}'",
            bool booleanValue => booleanValue ? "1" : "0",
            byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal => Convert.ToString(value, CultureInfo.InvariantCulture)!,
            Guid guid => $"'{guid:D}'",
            DateTime dateTime => $"'{dateTime:yyyy-MM-dd HH:mm:ss.fffffff}'",
            DateTimeOffset dateTimeOffset => $"'{dateTimeOffset.LocalDateTime:yyyy-MM-dd HH:mm:ss.fffffff}'",
            DateOnly dateOnly => $"'{dateOnly:yyyy-MM-dd}'",
            TimeOnly timeOnly => $"'{timeOnly:HH\\:mm\\:ss.fffffff}'",
            _ => throw new InvalidOperationException($"默认值类型 {value.GetType().FullName} 暂不支持自动格式化。")
        };
    }

    /// <summary>
    /// 转义 SQL 字符串字面量。
    /// </summary>
    /// <param name="literal">原始文本。</param>
    /// <returns>转义后的文本。</returns>
    private static string EscapeSqlLiteral(string literal)
    {
        return literal.Replace("'", "''", StringComparison.Ordinal);
    }

    /// <summary>
    /// 将字节长度转换为 Unicode 长度表达式。
    /// </summary>
    /// <param name="maxLength">字节长度。</param>
    /// <returns>长度表达式。</returns>
    private static string GetUnicodeLengthSql(short maxLength)
    {
        if (maxLength < 0)
        {
            return "MAX";
        }

        return (maxLength / 2).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 将字节长度转换为 SQL 长度表达式。
    /// </summary>
    /// <param name="maxLength">字节长度。</param>
    /// <returns>长度表达式。</returns>
    private static string GetLengthSql(short maxLength)
    {
        return maxLength < 0 ? "MAX" : maxLength.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 对标识符进行安全校验。
    /// </summary>
    /// <param name="identifier">标识符。</param>
    /// <param name="parameterName">参数名。</param>
    private static void ValidateIdentifier(string identifier, string parameterName)
    {
        if (!LogicalTableNameNormalizer.IsSafeSqlIdentifier(identifier))
        {
            throw new InvalidOperationException($"分表结构同步参数非法：{parameterName}='{identifier}'。仅允许字母、数字、下划线。");
        }
    }

    /// <summary>
    /// 对标识符进行安全引用。
    /// </summary>
    /// <param name="identifier">标识符。</param>
    /// <returns>加方括号后的安全标识符。</returns>
    private static string QuoteIdentifier(string identifier)
    {
        ValidateIdentifier(identifier, nameof(identifier));
        return $"[{identifier}]";
    }

    /// <summary>
    /// 物理索引行元数据。
    /// </summary>
    /// <param name="IndexName">索引名。</param>
    /// <param name="IsUnique">是否唯一。</param>
    /// <param name="KeyOrdinal">键列顺序。</param>
    /// <param name="ColumnName">列名。</param>
    private readonly record struct PhysicalIndexRow(string IndexName, bool IsUnique, int KeyOrdinal, string ColumnName);
}
