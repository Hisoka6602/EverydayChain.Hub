using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Services;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// RetentionExecutionService 行为测试。
/// </summary>
public class RetentionExecutionServiceTests
{
    /// <summary>
    /// 日志表名非法时应跳过并记录告警。
    /// </summary>
    [Fact]
    public async Task ExecuteRetentionCleanupAsync_WithInvalidLogTableName_ShouldSkipAndWarn()
    {
        var configRepository = new FakeSyncTaskConfigRepository([]);
        var resolver = new FakeShardTableResolver();
        resolver.SetPhysicalTables("scan_logs", ["scan_logs_202401"]);
        var retentionRepository = new FakeShardRetentionRepository();
        var logger = new TestLogger<RetentionExecutionService>();
        var service = new RetentionExecutionService(
            configRepository,
            resolver,
            retentionRepository,
            [
                new RetentionLogTableOptions
                {
                    Enabled = true,
                    LogicalTableName = "bad-name",
                    KeepMonths = 3,
                    DryRun = true,
                    AllowDrop = false
                },
                new RetentionLogTableOptions
                {
                    Enabled = true,
                    LogicalTableName = "scan_logs",
                    KeepMonths = 3,
                    DryRun = true,
                    AllowDrop = false
                }
            ],
            logger);

        var summary = await service.ExecuteRetentionCleanupAsync(CancellationToken.None);

        Assert.Contains("DryRun=1", summary, StringComparison.Ordinal);
        Assert.Equal(["scan_logs"], resolver.QueriedLogicalTables);
        Assert.Contains(logger.Logs, entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning
            && entry.Message.Contains("日志表保留期配置非法", StringComparison.Ordinal));
    }

    /// <summary>
    /// 日志表与同步表重复时应仅执行一次。
    /// </summary>
    [Fact]
    public async Task ExecuteRetentionCleanupAsync_WithDuplicatedLogicalTable_ShouldDeduplicate()
    {
        var syncTable = new SyncTableDefinition
        {
            TableCode = "T1",
            Enabled = true,
            TargetLogicalTable = "scan_logs",
            RetentionEnabled = true,
            RetentionKeepMonths = 3,
            RetentionDryRun = true,
            RetentionAllowDrop = false
        };
        var configRepository = new FakeSyncTaskConfigRepository([syncTable]);
        var resolver = new FakeShardTableResolver();
        resolver.SetPhysicalTables("scan_logs", ["scan_logs_202401"]);
        var retentionRepository = new FakeShardRetentionRepository();
        var logger = new TestLogger<RetentionExecutionService>();
        var service = new RetentionExecutionService(
            configRepository,
            resolver,
            retentionRepository,
            [
                new RetentionLogTableOptions
                {
                    Enabled = true,
                    LogicalTableName = "scan_logs",
                    KeepMonths = 6,
                    DryRun = false,
                    AllowDrop = true
                }
            ],
            logger);

        var summary = await service.ExecuteRetentionCleanupAsync(CancellationToken.None);

        Assert.Contains("Scanned=1", summary, StringComparison.Ordinal);
        Assert.Single(resolver.QueriedLogicalTables);
        Assert.Equal("scan_logs", resolver.QueriedLogicalTables[0]);
    }

    /// <summary>
    /// 同步配置仓储测试桩。
    /// </summary>
    /// <param name="tables">启用表集合。</param>
    private sealed class FakeSyncTaskConfigRepository(IReadOnlyList<SyncTableDefinition> tables) : ISyncTaskConfigRepository
    {
        /// <inheritdoc/>
        public Task<SyncTableDefinition> GetByTableCodeAsync(string tableCode, CancellationToken ct)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<SyncTableDefinition>> ListEnabledAsync(CancellationToken ct)
        {
            return Task.FromResult(tables);
        }

        /// <inheritdoc/>
        public Task<int> GetMaxParallelTablesAsync(CancellationToken ct)
        {
            return Task.FromResult(1);
        }
    }

    /// <summary>
    /// 分表解析仓储测试桩。
    /// </summary>
    private sealed class FakeShardTableResolver : IShardTableResolver
    {
        /// <summary>逻辑表到物理表映射。</summary>
        private readonly Dictionary<string, IReadOnlyList<string>> _tables = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>查询过的逻辑表名。</summary>
        public List<string> QueriedLogicalTables { get; } = [];

        /// <summary>
        /// 设置逻辑表映射。
        /// </summary>
        /// <param name="logicalTableName">逻辑表名。</param>
        /// <param name="physicalTables">物理表集合。</param>
        public void SetPhysicalTables(string logicalTableName, IReadOnlyList<string> physicalTables)
        {
            _tables[logicalTableName] = physicalTables;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<string>> ListPhysicalTablesAsync(string logicalTableName, CancellationToken ct)
        {
            QueriedLogicalTables.Add(logicalTableName);
            if (_tables.TryGetValue(logicalTableName, out var value))
            {
                return Task.FromResult(value);
            }

            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        /// <inheritdoc/>
        public DateTime? TryParseShardMonth(string physicalTableName)
        {
            var suffix = physicalTableName.Split('_').LastOrDefault();
            if (suffix is null || suffix.Length != 6 || !int.TryParse(suffix[..4], out var year) || !int.TryParse(suffix[4..], out var month))
            {
                return null;
            }

            return new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Local);
        }
    }

    /// <summary>
    /// 分表保留期仓储测试桩。
    /// </summary>
    private sealed class FakeShardRetentionRepository : IShardRetentionRepository
    {
        /// <inheritdoc/>
        public Task<string> GenerateRollbackScriptAsync(string logicalTableName, string physicalTableName, CancellationToken ct)
        {
            return Task.FromResult("rollback-script");
        }

        /// <inheritdoc/>
        public Task DropShardTableAsync(string logicalTableName, string physicalTableName, string rollbackScript, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
