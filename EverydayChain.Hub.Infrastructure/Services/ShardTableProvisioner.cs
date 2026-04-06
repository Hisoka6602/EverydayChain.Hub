using EverydayChain.Hub.Domain.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EverydayChain.Hub.SharedKernel.Utilities;

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
    private readonly IReadOnlyList<string> _managedLogicalTables = ValidateManagedLogicalTables(managedLogicalTables);
    /// <summary>分表预建并发上限。</summary>
    private readonly int _preProvisionMaxConcurrency = NormalizePreProvisionConcurrency(options.Value.PreProvisionMaxConcurrency);

    /// <inheritdoc/>
    public async Task EnsureShardTablesAsync(IEnumerable<string> suffixes, CancellationToken cancellationToken)
    {
        var semaphore = new SemaphoreSlim(_preProvisionMaxConcurrency, _preProvisionMaxConcurrency);
        var tasks = new List<Task>();
        foreach (var suffix in suffixes)
        {
            foreach (var logicalTable in _managedLogicalTables)
            {
                var tableCode = logicalTable;
                var shardSuffix = suffix;
                tasks.Add(RunWithConcurrencyAsync(
                    semaphore,
                    token => EnsureShardTableAsync(tableCode, shardSuffix, token),
                    cancellationToken));
            }
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        finally
        {
            semaphore.Dispose();
        }
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

    /// <summary>
    /// 校验并冻结纳管逻辑表集合，防止运行时注入非法标识符。
    /// </summary>
    /// <param name="managedLogicalTables">待校验逻辑表集合。</param>
    /// <returns>校验通过的只读逻辑表列表。</returns>
    /// <exception cref="InvalidOperationException">存在空值或非法标识符时抛出。</exception>
    private static IReadOnlyList<string> ValidateManagedLogicalTables(IReadOnlyList<string> managedLogicalTables)
    {
        ArgumentNullException.ThrowIfNull(managedLogicalTables);
        if (managedLogicalTables.Count == 0)
        {
            throw new InvalidOperationException("分表配置无效：纳管逻辑表集合为空，无法执行分表预建。");
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
    /// 归一化并发上限配置值。
    /// </summary>
    /// <param name="configuredConcurrency">配置并发上限。</param>
    /// <returns>限制到 1-64 范围后的并发值。</returns>
    private static int NormalizePreProvisionConcurrency(int configuredConcurrency)
    {
        return Math.Clamp(configuredConcurrency, 1, 64);
    }

    /// <summary>
    /// 在信号量限制下执行异步任务。
    /// </summary>
    /// <param name="semaphore">并发控制信号量。</param>
    /// <param name="action">待执行动作。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    private static async Task RunWithConcurrencyAsync(
        SemaphoreSlim semaphore,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            await action(cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    }

}
