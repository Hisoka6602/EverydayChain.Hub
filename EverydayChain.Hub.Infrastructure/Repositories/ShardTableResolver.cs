using System.Globalization;
using System.Text.RegularExpressions;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 定义 ShardTableResolver 类型。
/// </summary>
public class ShardTableResolver(IOptions<ShardingOptions> shardingOptions) : IShardTableResolver
{
    /// <summary>
    /// 存储 ResolveCommandTimeoutSeconds 字段。
    /// </summary>
    private const int ResolveCommandTimeoutSeconds = 15;
    private static readonly Regex SqlIdentifierRegex = new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);
    private static readonly Regex ShardMonthRegex = new(@"_(\d{6})$", RegexOptions.Compiled);
    /// <summary>
    /// 存储 _options 字段。
    /// </summary>
    private readonly ShardingOptions _options = shardingOptions.Value;

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
        command.CommandTimeout = ResolveCommandTimeoutSeconds;
        command.Parameters.AddWithValue("@schema", _options.Schema);
        command.Parameters.AddWithValue("@prefix", logicalTableName);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

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

    private static bool IsSafeSqlIdentifier(string identifier)
    {
        return !string.IsNullOrWhiteSpace(identifier) && SqlIdentifierRegex.IsMatch(identifier);
    }
}

