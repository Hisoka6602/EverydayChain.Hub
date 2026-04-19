using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Host.Contracts.Requests;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Globalization;
using System.Text.RegularExpressions;

namespace EverydayChain.Hub.Host.Swagger;

/// <summary>
/// 业务任务模拟补数请求的目标表名下拉增强。
/// </summary>
public sealed class BusinessTaskSeedTableSchemaFilter : ISchemaFilter
{
    /// <summary>
    /// 业务任务分表名前缀。
    /// </summary>
    private const string BusinessTaskTablePrefix = "business_tasks";

    /// <summary>
    /// 业务任务分表名匹配正则。
    /// </summary>
    private static readonly Regex BusinessTaskTableNameRegex = new("^business_tasks_(\\d{6})$", RegexOptions.Compiled);

    /// <summary>
    /// 分表解析仓储。
    /// </summary>
    private readonly IShardTableResolver _shardTableResolver;

    /// <summary>
    /// 日志记录器。
    /// </summary>
    private readonly ILogger<BusinessTaskSeedTableSchemaFilter> _logger;

    /// <summary>
    /// 初始化业务任务模拟补数表名下拉增强。
    /// </summary>
    /// <param name="shardTableResolver">分表解析仓储。</param>
    /// <param name="logger">日志记录器。</param>
    public BusinessTaskSeedTableSchemaFilter(
        IShardTableResolver shardTableResolver,
        ILogger<BusinessTaskSeedTableSchemaFilter> logger)
    {
        _shardTableResolver = shardTableResolver;
        _logger = logger;
    }

    /// <inheritdoc/>
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(BusinessTaskSeedRequest))
        {
            return;
        }

        if (!schema.Properties.TryGetValue("targetTableName", out var targetTableSchema)
            && !schema.Properties.TryGetValue(nameof(BusinessTaskSeedRequest.TargetTableName), out targetTableSchema))
        {
            return;
        }

        try
        {
            var tableNames = _shardTableResolver
                .ListPhysicalTablesAsync(BusinessTaskTablePrefix, CancellationToken.None)
                .GetAwaiter()
                .GetResult()
                .Where(IsValidBusinessTaskTableName)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();
            var currentMonthTableName = $"business_tasks_{DateTime.Now:yyyyMM}";
            if (!tableNames.Contains(currentMonthTableName, StringComparer.Ordinal))
            {
                tableNames.Add(currentMonthTableName);
            }

            if (tableNames.Count == 0)
            {
                return;
            }

            targetTableSchema.Enum = tableNames
                .Select(tableName => (IOpenApiAny)new OpenApiString(tableName))
                .ToList();
            targetTableSchema.Example = new OpenApiString(tableNames[0]);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "业务任务模拟补数目标表下拉加载失败，已降级为普通字符串输入。");
        }
    }

    /// <summary>
    /// 判断表名是否为合法业务任务分表名。
    /// </summary>
    /// <param name="tableName">待校验表名。</param>
    /// <returns>合法返回 true，否则返回 false。</returns>
    private static bool IsValidBusinessTaskTableName(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return false;
        }

        var match = BusinessTaskTableNameRegex.Match(tableName);
        if (!match.Success)
        {
            return false;
        }

        return DateTime.TryParseExact(
            match.Groups[1].Value + "01",
            "yyyyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal,
            out _);
    }
}
