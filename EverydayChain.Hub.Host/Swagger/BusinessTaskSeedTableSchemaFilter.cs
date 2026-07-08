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
/// 定义 BusinessTaskSeedTableSchemaFilter 类型。
/// </summary>
public sealed class BusinessTaskSeedTableSchemaFilter : ISchemaFilter
{
    /// <summary>
    /// 存储 BusinessTaskTablePrefix 字段。
    /// </summary>
    private const string BusinessTaskTablePrefix = "business_tasks";

    /// <summary>
    /// 存储业务任务物理表名匹配正则。
    /// </summary>
    private static readonly Regex BusinessTaskTableNameRegex = new("^business_tasks_(\\d{6})$", RegexOptions.Compiled);

    /// <summary>
    /// 存储 _shardTableResolver 字段。
    /// </summary>
    private readonly IShardTableResolver _shardTableResolver;

    /// <summary>
    /// 存储 _logger 字段。
    /// </summary>
    private readonly ILogger<BusinessTaskSeedTableSchemaFilter> _logger;

    /// <summary>
    /// 执行 BusinessTaskSeedTableSchemaFilter 方法。
    /// </summary>
    public BusinessTaskSeedTableSchemaFilter(
        IShardTableResolver shardTableResolver,
        ILogger<BusinessTaskSeedTableSchemaFilter> logger)
    {
        // 步骤：执行 BusinessTaskSeedTableSchemaFilter 方法的核心处理流程。
        _shardTableResolver = shardTableResolver;
        _logger = logger;
    }

    /// <summary>
    /// 为业务任务模拟补数请求填充目标表下拉枚举。
    /// </summary>
    /// <param name="schema">当前架构对象。</param>
    /// <param name="context">架构过滤上下文。</param>
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
                .ToList();
            var currentMonthTableName = $"business_tasks_{DateTime.Now:yyyyMM}";
            if (!tableNames.Contains(currentMonthTableName, StringComparer.Ordinal))
            {
                tableNames.Add(currentMonthTableName);
            }
            tableNames = tableNames
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

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

