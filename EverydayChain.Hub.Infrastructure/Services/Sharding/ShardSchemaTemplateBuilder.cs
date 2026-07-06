using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Services.Sharding.Metadata;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EverydayChain.Hub.Infrastructure.Services.Sharding;

/// <summary>
/// 定义当前类型。
/// </summary>
internal static class ShardSchemaTemplateBuilder
{
    internal static IReadOnlyList<string> ValidateManagedLogicalTables(IReadOnlyList<string> managedLogicalTables)
    {
        ArgumentNullException.ThrowIfNull(managedLogicalTables);
        if (managedLogicalTables.Count == 0)
        {
            throw new InvalidOperationException("分表配置无效：纳管逻辑表集合为空，无法执行分表治理。");
        }

        foreach (var logicalTable in managedLogicalTables)
        {
            if (!LogicalTableNameNormalizer.IsSafeSqlIdentifier(logicalTable))
            {
                throw new InvalidOperationException($"分表配置无效：逻辑表名 '{logicalTable}' 非法，仅允许字母、数字、下划线。");
            }
        }

        return managedLogicalTables.ToArray();
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    internal static IReadOnlyDictionary<string, ShardTableSchemaTemplate> BuildTableTemplates(
        IDbContextFactory<HubDbContext> dbContextFactory,
        IReadOnlyList<string> managedLogicalTables)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        var validatedLogicalTables = ValidateManagedLogicalTables(managedLogicalTables);
        var result = new Dictionary<string, ShardTableSchemaTemplate>(StringComparer.OrdinalIgnoreCase);

        using var _ = TableSuffixScope.Use(string.Empty);
        using var dbContext = dbContextFactory.CreateDbContext();
        var model = dbContext.Model;
        foreach (var logicalTable in validatedLogicalTables)
        {
            var entityType = model
                .GetEntityTypes()
                .FirstOrDefault(entity => string.Equals(entity.GetTableName(), logicalTable, StringComparison.OrdinalIgnoreCase));
            if (entityType is null)
            {
                throw new InvalidOperationException($"分表配置无效：逻辑表名 '{logicalTable}' 未找到对应实体模型映射。");
            }

            var tableIdentifier = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());
            var columns = entityType
                .GetProperties()
                .Select((property, index) => BuildColumn(property, tableIdentifier, index))
                .OrderBy(column => column.Ordinal)
                .ToList();
            var primaryKeyColumns = entityType.FindPrimaryKey()?.Properties
                .Select(property => property.GetColumnName(tableIdentifier) ?? property.Name)
                .ToList() ?? [];
            var indexes = entityType
                .GetIndexes()
                .Select(index => new ShardIndexSchema(
                    index.GetDatabaseName() ?? $"IX_{entityType.GetTableName()}_{string.Join("_", index.Properties.Select(property => property.Name))}",
                    index.IsUnique,
                    index.Properties.Select(property => property.GetColumnName(tableIdentifier) ?? property.Name).ToList()))
                .Where(index => index.ColumnNames.Count > 0)
                .ToList();

            result[logicalTable] = new ShardTableSchemaTemplate(
                logicalTable,
                entityType.GetSchema() ?? "dbo",
                columns,
                primaryKeyColumns,
                indexes);
        }

        return result;
    }

    internal static string BuildPhysicalIndexName(string logicalTable, string physicalTableName, string databaseName)
    {
        return databaseName.Replace(logicalTable, physicalTableName, StringComparison.OrdinalIgnoreCase);
    }

    private static ShardColumnSchema BuildColumn(IProperty property, StoreObjectIdentifier tableIdentifier, int ordinal)
    {
        return new ShardColumnSchema(
            property.GetColumnName(tableIdentifier) ?? property.Name,
            ResolveStoreType(property),
            property.IsNullable,
            IsIdentity(property),
            property.GetDefaultValueSql(),
            property.GetDefaultValue(),
            ordinal);
    }

    private static bool IsIdentity(IProperty property)
    {
        if (property.ValueGenerated != ValueGenerated.OnAdd)
        {
            return false;
        }

        var underlyingType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
        return underlyingType == typeof(int) || underlyingType == typeof(long);
    }

    private static string ResolveStoreType(IProperty property)
    {
        var configuredStoreType = property.GetColumnType();
        if (!string.IsNullOrWhiteSpace(configuredStoreType))
        {
            return configuredStoreType;
        }

        var relationalTypeMapping = property.GetRelationalTypeMapping();
        var storeType = relationalTypeMapping?.StoreType;
        if (!string.IsNullOrWhiteSpace(storeType))
        {
            return storeType;
        }

        throw new InvalidOperationException($"分表模板解析失败：属性 {property.DeclaringType.DisplayName()}.{property.Name} 缺少可识别的 SQL 类型映射。请检查该属性是否配置了 Column(TypeName=...) 或 Fluent API 类型映射。");
    }
}

