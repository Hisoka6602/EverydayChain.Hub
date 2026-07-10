using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Services;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Domain.Runtime;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 RetentionExecutionServiceTests 类型。
/// </summary>
public class RetentionExecutionServiceTests
{
    [Fact]
    public async Task ExecuteRetentionCleanupAsync_WithInvalidLogTableName_ShouldSkipAndWarn()
    {
        var configRepository = new FakeSyncTaskConfigRepository([]);
        var resolver = new FakeShardTableResolver();
        resolver.SetPhysicalTables("scan_logs", ["scan_logs_202401"]);
        var retentionRepository = new FakeShardRetentionRepository();
        var runtimeLeaseRepository = new FakeRuntimeLeaseRepository();
        var retentionAuditLogRepository = new InMemoryRetentionCleanupAuditLogRepository();
        var logger = new TestLogger<RetentionExecutionService>();
        var service = new RetentionExecutionService(
            configRepository,
            resolver,
            retentionRepository,
            runtimeLeaseRepository,
            retentionAuditLogRepository,
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
            Microsoft.Extensions.Options.Options.Create(new RetentionJobOptions()),
            logger);

        var summary = await service.ExecuteRetentionCleanupAsync(CancellationToken.None);

        Assert.Contains("预演 1 项", summary, StringComparison.Ordinal);
        Assert.Equal(["scan_logs"], resolver.QueriedLogicalTables);
        Assert.Contains(logger.Logs, entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning
            && entry.Message.Contains("日志表保留期配置非法", StringComparison.Ordinal));
    }

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
        var runtimeLeaseRepository = new FakeRuntimeLeaseRepository();
        var retentionAuditLogRepository = new InMemoryRetentionCleanupAuditLogRepository();
        var logger = new TestLogger<RetentionExecutionService>();
        var service = new RetentionExecutionService(
            configRepository,
            resolver,
            retentionRepository,
            runtimeLeaseRepository,
            retentionAuditLogRepository,
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
            Microsoft.Extensions.Options.Options.Create(new RetentionJobOptions()),
            logger);

        var summary = await service.ExecuteRetentionCleanupAsync(CancellationToken.None);

        Assert.Contains("扫描 1 个目标", summary, StringComparison.Ordinal);
        Assert.Single(resolver.QueriedLogicalTables);
        Assert.Equal("scan_logs", resolver.QueriedLogicalTables[0]);
    }

    [Fact]
    public async Task ExecuteRetentionCleanupAsync_WhenLeaseIsHeldByAnotherInstance_ShouldSkipExecution()
    {
        var configRepository = new FakeSyncTaskConfigRepository([]);
        var resolver = new FakeShardTableResolver();
        var retentionRepository = new FakeShardRetentionRepository();
        var runtimeLeaseRepository = new FakeRuntimeLeaseRepository
        {
            TryAcquireResult = false
        };
        var retentionAuditLogRepository = new InMemoryRetentionCleanupAuditLogRepository();
        var logger = new TestLogger<RetentionExecutionService>();
        var service = new RetentionExecutionService(
            configRepository,
            resolver,
            retentionRepository,
            runtimeLeaseRepository,
            retentionAuditLogRepository,
            [
                new RetentionLogTableOptions
                {
                    Enabled = true,
                    LogicalTableName = "scan_logs",
                    KeepMonths = 3,
                    DryRun = false,
                    AllowDrop = true
                }
            ],
            Microsoft.Extensions.Options.Options.Create(new RetentionJobOptions()),
            logger);

        var summary = await service.ExecuteRetentionCleanupAsync(CancellationToken.None);

        Assert.Contains("跳过", summary, StringComparison.Ordinal);
        Assert.Empty(resolver.QueriedLogicalTables);
        Assert.Empty(retentionAuditLogRepository.Items);
        Assert.Equal(1, runtimeLeaseRepository.TryAcquireCallCount);
        Assert.Equal(0, runtimeLeaseRepository.ReleaseCallCount);
    }

    [Fact]
    public async Task ExecuteRetentionCleanupAsync_WithDeleteRowsMode_ShouldDeleteAndWriteAudit()
    {
        var configRepository = new FakeSyncTaskConfigRepository([]);
        var resolver = new FakeShardTableResolver();
        var retentionRepository = new FakeShardRetentionRepository
        {
            CountRowsBeforeResult = 5,
            DeleteRowsBeforeResult = 5
        };
        var runtimeLeaseRepository = new FakeRuntimeLeaseRepository();
        var retentionAuditLogRepository = new InMemoryRetentionCleanupAuditLogRepository();
        var logger = new TestLogger<RetentionExecutionService>();
        var service = new RetentionExecutionService(
            configRepository,
            resolver,
            retentionRepository,
            runtimeLeaseRepository,
            retentionAuditLogRepository,
            [
                new RetentionLogTableOptions
                {
                    Enabled = true,
                    LogicalTableName = "dashboard_task_snapshots",
                    RetentionMode = "DeleteRows",
                    TimeColumnName = "BucketStartLocal",
                    KeepMonths = 3,
                    DryRun = false,
                    AllowDrop = true,
                    DeleteBatchSize = 1000
                }
            ],
            Microsoft.Extensions.Options.Options.Create(new RetentionJobOptions
            {
                PollingIntervalSeconds = 3600
            }),
            logger);

        var summary = await service.ExecuteRetentionCleanupAsync(CancellationToken.None);

        Assert.Contains("删除 5 项", summary, StringComparison.Ordinal);
        Assert.Equal(1, retentionRepository.CountRowsBeforeCallCount);
        Assert.Equal(1, retentionRepository.DeleteRowsBeforeCallCount);
        Assert.Single(retentionAuditLogRepository.Items);
        var auditLog = retentionAuditLogRepository.Items[0];
        Assert.Equal("Completed", auditLog.ExecutionStage);
        Assert.Equal(1, auditLog.ScannedCount);
        Assert.Equal(5, auditLog.CandidateCount);
        Assert.Equal(5, auditLog.DeletedCount);
        Assert.Equal("dashboard_task_snapshots", auditLog.LogicalTableName);
        Assert.Equal(1, runtimeLeaseRepository.ReleaseCallCount);
    }

    /// <summary>
    /// 定义 FakeSyncTaskConfigRepository 类型。
    /// </summary>
    private sealed class FakeSyncTaskConfigRepository(IReadOnlyList<SyncTableDefinition> tables) : ISyncTaskConfigRepository
    {
        public Task<SyncTableDefinition> GetByTableCodeAsync(string tableCode, CancellationToken ct)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<SyncTableDefinition>> ListEnabledAsync(CancellationToken ct)
        {
            return Task.FromResult(tables);
        }

        public Task<int> GetMaxParallelTablesAsync(CancellationToken ct)
        {
            return Task.FromResult(1);
        }
    }

    /// <summary>
    /// 定义 FakeShardTableResolver 类型。
    /// </summary>
    private sealed class FakeShardTableResolver : IShardTableResolver
    {
        private readonly Dictionary<string, IReadOnlyList<string>> _tables = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 获取或设置 QueriedLogicalTables。
        /// </summary>
        public List<string> QueriedLogicalTables { get; } = [];

        public void SetPhysicalTables(string logicalTableName, IReadOnlyList<string> physicalTables)
        {
            _tables[logicalTableName] = physicalTables;
        }

        public Task<IReadOnlyList<string>> ListPhysicalTablesAsync(string logicalTableName, CancellationToken ct)
        {
            QueriedLogicalTables.Add(logicalTableName);
            if (_tables.TryGetValue(logicalTableName, out var value))
            {
                return Task.FromResult(value);
            }

            return Task.FromResult<IReadOnlyList<string>>([]);
        }

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
    /// 模拟保留期仓储，供单元测试验证去重与跳过逻辑使用。
    /// </summary>
    private sealed class FakeShardRetentionRepository : IShardRetentionRepository
    {
        /// <summary>
        /// 获取或设置统计过期行数的固定返回值。
        /// </summary>
        public int CountRowsBeforeResult { get; set; }

        /// <summary>
        /// 获取或设置批量删行的固定返回值。
        /// </summary>
        public int DeleteRowsBeforeResult { get; set; }

        /// <summary>
        /// 获取或设置统计过期行数的调用次数。
        /// </summary>
        public int CountRowsBeforeCallCount { get; private set; }

        /// <summary>
        /// 获取或设置批量删行的调用次数。
        /// </summary>
        public int DeleteRowsBeforeCallCount { get; private set; }

        /// <summary>
        /// 返回固定回滚脚本文本，供测试覆盖旧分表删除分支。
        /// </summary>
        /// <param name="logicalTableName">逻辑表名。</param>
        /// <param name="physicalTableName">物理分表名。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>固定回滚脚本。</returns>
        public Task<string> GenerateRollbackScriptAsync(string logicalTableName, string physicalTableName, CancellationToken ct)
        {
            return Task.FromResult("rollback-script");
        }

        /// <summary>
        /// 模拟删除旧分表，不执行任何真实数据库操作。
        /// </summary>
        /// <param name="logicalTableName">逻辑表名。</param>
        /// <param name="physicalTableName">物理分表名。</param>
        /// <param name="rollbackScript">回滚脚本。</param>
        /// <param name="ct">取消令牌。</param>
        public Task DropShardTableAsync(string logicalTableName, string physicalTableName, string rollbackScript, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 模拟统计固定表中过期数据行数，当前测试场景统一返回零。
        /// </summary>
        /// <param name="tableName">固定表名。</param>
        /// <param name="timeColumnName">时间列名。</param>
        /// <param name="thresholdTimeLocal">本地时间阈值。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>固定返回零，表示没有过期数据。</returns>
        public Task<int> CountRowsBeforeAsync(string tableName, string timeColumnName, DateTime thresholdTimeLocal, CancellationToken ct)
        {
            CountRowsBeforeCallCount++;
            return Task.FromResult(CountRowsBeforeResult);
        }

        /// <summary>
        /// 模拟批量删除固定表中过期数据，当前测试场景不删除任何记录。
        /// </summary>
        /// <param name="tableName">固定表名。</param>
        /// <param name="timeColumnName">时间列名。</param>
        /// <param name="thresholdTimeLocal">本地时间阈值。</param>
        /// <param name="batchSize">单批删除行数。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>固定返回零，表示未删除任何记录。</returns>
        public Task<int> DeleteRowsBeforeAsync(string tableName, string timeColumnName, DateTime thresholdTimeLocal, int batchSize, CancellationToken ct)
        {
            DeleteRowsBeforeCallCount++;
            return Task.FromResult(DeleteRowsBeforeResult);
        }
    }

    /// <summary>
    /// 模拟运行租约仓储，供测试验证保留期任务的互斥行为。
    /// </summary>
    private sealed class FakeRuntimeLeaseRepository : IRuntimeLeaseRepository
    {
        /// <summary>
        /// 获取或设置抢占租约的固定返回值。
        /// </summary>
        public bool TryAcquireResult { get; set; } = true;

        /// <summary>
        /// 获取或设置抢占租约调用次数。
        /// </summary>
        public int TryAcquireCallCount { get; private set; }

        /// <summary>
        /// 获取或设置释放租约调用次数。
        /// </summary>
        public int ReleaseCallCount { get; private set; }

        /// <summary>
        /// 模拟抢占指定租约。
        /// </summary>
        /// <param name="leaseKey">租约键。</param>
        /// <param name="ownerId">持有者标识。</param>
        /// <param name="acquiredTimeLocal">抢占时间。</param>
        /// <param name="expiresAtLocal">过期时间。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>固定返回预设结果。</returns>
        public Task<bool> TryAcquireAsync(string leaseKey, string ownerId, DateTime acquiredTimeLocal, DateTime expiresAtLocal, CancellationToken ct)
        {
            TryAcquireCallCount++;
            return Task.FromResult(TryAcquireResult);
        }

        /// <summary>
        /// 模拟读取指定租约快照。
        /// </summary>
        /// <param name="leaseKey">租约键。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>测试场景固定返回空快照。</returns>
        public Task<RuntimeLeaseSnapshot?> GetAsync(string leaseKey, CancellationToken ct)
        {
            return Task.FromResult<RuntimeLeaseSnapshot?>(null);
        }

        /// <summary>
        /// 模拟释放指定租约。
        /// </summary>
        /// <param name="leaseKey">租约键。</param>
        /// <param name="ownerId">持有者标识。</param>
        /// <param name="ct">取消令牌。</param>
        public Task ReleaseAsync(string leaseKey, string ownerId, CancellationToken ct)
        {
            ReleaseCallCount++;
            return Task.CompletedTask;
        }
    }
}

