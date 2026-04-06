using EverydayChain.Hub.Domain.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 分表预置服务实现，在 SQL Server 中按需创建分表与索引。
/// </summary>
public class ShardTableProvisioner(
    IOptions<ShardingOptions> options,
    IReadOnlyList<string> managedLogicalTables,
    ILogger<ShardTableProvisioner> logger,
    IDangerZoneExecutor dangerZoneExecutor) : IShardTableProvisioner
{
    /// <summary>分表配置快照。</summary>
    private readonly ShardingOptions _options = options.Value;
    /// <summary>纳管逻辑表列表。</summary>
    private readonly IReadOnlyList<string> _managedLogicalTables = managedLogicalTables;

    /// <inheritdoc/>
    public Task EnsureShardTablesAsync(IEnumerable<string> suffixes, CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();
        foreach (var suffix in suffixes)
        {
            foreach (var logicalTable in _managedLogicalTables)
            {
                tasks.Add(EnsureShardTableAsync(logicalTable, suffix, cancellationToken));
            }
        }

        return Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public Task EnsureShardTableAsync(string suffix, CancellationToken cancellationToken)
    {
        var tasks = _managedLogicalTables.Select(logicalTable => EnsureShardTableAsync(logicalTable, suffix, cancellationToken));
        return Task.WhenAll(tasks);
    }

    /// <summary>
    /// 确保指定逻辑表与后缀对应的物理分表存在。
    /// </summary>
    /// <param name="logicalTable">逻辑表名。</param>
    /// <param name="suffix">分表后缀。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    private Task EnsureShardTableAsync(string logicalTable, string suffix, CancellationToken cancellationToken) => dangerZoneExecutor.ExecuteAsync(
        $"ensure-shard-table-{logicalTable}-{suffix}",
        async token =>
        {
            var tableName = $"{logicalTable}{suffix}";
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
