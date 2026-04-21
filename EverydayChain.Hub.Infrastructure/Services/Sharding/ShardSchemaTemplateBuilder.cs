using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Services.Sharding.Metadata;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EverydayChain.Hub.Infrastructure.Services.Sharding;

/// <summary>
/// 分表结构模板构建器。
/// </summary>
internal static class ShardSchemaTemplateBuilder
{
    /// <summary>
    /// 校验并冻结纳管逻辑表集合，防止运行时注入非法标识符。
    /// </summary>
    /// <param name="managedLogicalTables">待校验逻辑表集合。</param>
    /// <returns>校验通过的只读逻辑表列表。</returns>
    /// <exception cref="InvalidOperationException">存在空值或非法标识符时抛出。</exception>
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

        return managedLogicalTables;
    }

    /// <summary>
    /// 基于 EF 实体模型构建逻辑表模板缓存。
    /// </summary>
    /// <param name="dbContextFactory">DbContext 工厂。</param>
    /// <param name="managedLogicalTables">纳管逻辑表集合。</param>
    /// <returns>逻辑表模板字典。</returns>
    internal static IReadOnlyDictionary<string, ShardTableSchemaTemplate> BuildTableTemplates(
        IDbContextFactory<HubDbContext> dbContextFactory,
        IReadOnlyList<string> managedLogicalTables)
    {
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

    /// <summary>
    /// 将逻辑索引名映射为物理分表索引名。
    /// </summary>
    /// <param name="logicalTable">逻辑表名。</param>
    /// <param name="physicalTableName">物理表名。</param>
    /// <param name="databaseName">逻辑索引名。</param>
    /// <returns>物理索引名。</returns>
    internal static string BuildPhysicalIndexName(string logicalTable, string physicalTableName, string databaseName)
    {
        return databaseName.Replace(logicalTable, physicalTableName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 解析属性对应的分表列模板。
    /// </summary>
    /// <param name="property">实体属性元数据。</param>
    /// <param name="tableIdentifier">表标识。</param>
    /// <param name="ordinal">列顺序。</param>
    /// <returns>列模板。</returns>
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

    /// <summary>
    /// 判断属性是否为自增列。
    /// </summary>
    /// <param name="property">实体属性元数据。</param>
    /// <returns>若为自增列返回 true，否则返回 false。</returns>
    private static bool IsIdentity(IProperty property)
    {
        if (property.ValueGenerated != ValueGenerated.OnAdd)
        {
            return false;
        }

        var underlyingType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
        return underlyingType == typeof(int) || underlyingType == typeof(long);
    }

    /// <summary>
    /// 解析属性对应的关系型存储类型。
    /// </summary>
    /// <param name="property">实体属性元数据。</param>
    /// <returns>SQL Server 存储类型。</returns>
    /// <exception cref="InvalidOperationException">缺少可识别的映射时抛出。</exception>
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

        throw new InvalidOperationException($"分表模板解析失败：属性 {property.DeclaringType.DisplayName()}.{property.Name} 缺少可识别的 SQL 类型映射。");
    }
}
