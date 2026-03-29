using EverydayChain.Hub.Application.Repositories;
using EverydayChain.Hub.Infrastructure.Options;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
}
