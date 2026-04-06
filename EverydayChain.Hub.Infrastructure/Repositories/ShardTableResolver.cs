using System.Globalization;
using System.Text.RegularExpressions;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 分表解析仓储实现。
/// </summary>
public class ShardTableResolver(IOptions<ShardingOptions> shardingOptions) : IShardTableResolver
{
    /// <summary>安全标识符校验正则（仅允许字母、数字、下划线）。</summary>
    private static readonly Regex SqlIdentifierRegex = new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);
    /// <summary>分表月份解析正则。</summary>
    private static readonly Regex ShardMonthRegex = new(@"_(\d{6})$", RegexOptions.Compiled);
    /// <summary>分表配置快照。</summary>
    private readonly ShardingOptions _options = shardingOptions.Value;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> ListPhysicalTablesAsync(string logicalTableName, CancellationToken ct)
    {
        if (!IsSafeSqlIdentifier(logicalTableName))
        {
            throw new InvalidOperationException($"逻辑表名不合法：{logicalTableName}");
        }

        var tables = new List<string>();
        var sql = """
                  SELECT [TABLE_NAME]
                  FROM [INFORMATION_SCHEMA].[TABLES]
                  WHERE [TABLE_SCHEMA] = @schema
                    AND [TABLE_TYPE] = 'BASE TABLE'
                    AND [TABLE_NAME] LIKE @prefix + '[_]%'
                  ORDER BY [TABLE_NAME];
                  """;
        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@schema", _options.Schema);
        command.Parameters.AddWithValue("@prefix", logicalTableName);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    /// <inheritdoc/>
    public DateTime? TryParseShardMonth(string physicalTableName)
    {
        var match = ShardMonthRegex.Match(physicalTableName);
        if (!match.Success)
        {
            return null;
        }

        if (!DateTime.TryParseExact(
                match.Groups[1].Value + "01",
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out var parsedMonth))
        {
            return null;
        }

        return DateTime.SpecifyKind(parsedMonth, DateTimeKind.Local);
    }

    /// <summary>
    /// 判断标识符是否满足安全规则。
    /// </summary>
    /// <param name="identifier">待校验标识符。</param>
    /// <returns>合法返回 <c>true</c>。</returns>
    private static bool IsSafeSqlIdentifier(string identifier)
    {
        return !string.IsNullOrWhiteSpace(identifier) && SqlIdentifierRegex.IsMatch(identifier);
    }
}
