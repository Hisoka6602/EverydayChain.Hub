using EverydayChain.Hub.Infrastructure.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 分表预置服务实现，在 SQL Server 中按需创建分表与索引。
/// </summary>
public class ShardTableProvisioner(IOptions<ShardingOptions> options, ILogger<ShardTableProvisioner> logger, IDangerZoneExecutor dangerZoneExecutor) : IShardTableProvisioner
{
    /// <summary>分表配置快照。</summary>
    private readonly ShardingOptions _options = options.Value;

    /// <inheritdoc/>
    public Task EnsureShardTablesAsync(IEnumerable<string> suffixes, CancellationToken cancellationToken)
    {
        var tasks = suffixes.Select(x => EnsureShardTableAsync(x, cancellationToken));
        return Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public Task EnsureShardTableAsync(string suffix, CancellationToken cancellationToken) => dangerZoneExecutor.ExecuteAsync(
        $"ensure-shard-table-{suffix}",
        async token =>
        {
            var tableName = $"{_options.BaseTableName}{suffix}";
            var fullName = $"[{_options.Schema}].[{tableName}]";

            // 生成幂等建表 DDL：仅在表不存在时创建表及索引。
            var sql = $@"
IF OBJECT_ID(N'{_options.Schema}.{tableName}', N'U') IS NULL
BEGIN
    CREATE TABLE {fullName} (
        [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [BusinessNo] NVARCHAR(32) NOT NULL,
        [Channel] NVARCHAR(32) NOT NULL,
        [StationCode] NVARCHAR(64) NOT NULL,
        [Status] NVARCHAR(32) NOT NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL,
        [Payload] NVARCHAR(512) NULL
    );
    CREATE INDEX [IX_{tableName}_BusinessNo] ON {fullName}([BusinessNo]);
    CREATE INDEX [IX_{tableName}_CreatedAt] ON {fullName}([CreatedAt]);
END";

            await using var connection = new SqlConnection(_options.ConnectionString);
            await connection.OpenAsync(token);
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(token);
            logger.LogInformation("分表自治: 已确认分表存在 {Table}", fullName);
        },
        cancellationToken);
}
