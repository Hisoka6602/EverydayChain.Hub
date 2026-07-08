using EverydayChain.Hub.Domain.Aggregates.AuditLogs;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Aggregates.DashboardSnapshotAggregate;
using EverydayChain.Hub.Domain.Aggregates.DropLogAggregate;
using EverydayChain.Hub.Domain.Aggregates.RuntimeLeaseAggregate;
using EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;
using EverydayChain.Hub.Domain.Aggregates.SortingTaskTraceAggregate;
using EverydayChain.Hub.Domain.Aggregates.SyncBatchAggregate;
using EverydayChain.Hub.Domain.Aggregates.SyncChangeLogAggregate;
using EverydayChain.Hub.Domain.Aggregates.SyncDeletionLogAggregate;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence.EntityConfigurations;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Persistence;

/// <summary>
/// 定义 HubDbContext 类型。
/// </summary>
public sealed class HubDbContext : DbContext
{
    /// <summary>
    /// 存储 SortingTaskTraceLogicalTable 字段。
    /// </summary>
    private const string SortingTaskTraceLogicalTable = "sorting_task_trace";

    /// <summary>
    /// 存储 BusinessTaskLogicalTable 字段。
    /// </summary>
    private const string BusinessTaskLogicalTable = "business_tasks";

    /// <summary>
    /// 存储 ScanLogLogicalTable 字段。
    /// </summary>
    private const string ScanLogLogicalTable = "scan_logs";

    /// <summary>
    /// 存储 DropLogLogicalTable 字段。
    /// </summary>
    private const string DropLogLogicalTable = "drop_logs";

    /// <summary>
    /// 存储 SyncBatchLogicalTable 字段。
    /// </summary>
    private const string SyncBatchLogicalTable = "sync_batches";

    /// <summary>
    /// 存储 SyncChangeLogLogicalTable 字段。
    /// </summary>
    private const string SyncChangeLogLogicalTable = "sync_change_logs";

    /// <summary>
    /// 存储 SyncDeletionLogLogicalTable 字段。
    /// </summary>
    private const string SyncDeletionLogLogicalTable = "sync_deletion_logs";

    /// <summary>
    /// 存储 WaveCleanupAuditLogTableName 字段。
    /// </summary>
    private const string WaveCleanupAuditLogTableName = "wave_cleanup_audit_logs";

    /// <summary>
    /// 获取保留期清理审计表名。
    /// </summary>
    private const string RetentionCleanupAuditLogTableName = "retention_cleanup_audit_logs";

    /// <summary>
    /// 存储 RuntimeLeaseTableName 字段。
    /// </summary>
    private const string RuntimeLeaseTableName = "runtime_leases";

    /// <summary>
    /// 存储 DashboardTaskSnapshotTableName 字段。
    /// </summary>
    private const string DashboardTaskSnapshotTableName = "dashboard_task_snapshots";

    /// <summary>
    /// 存储 DashboardScanSnapshotTableName 字段。
    /// </summary>
    private const string DashboardScanSnapshotTableName = "dashboard_scan_snapshots";

    /// <summary>
    /// 存储 DashboardCurrentWaveSnapshotTableName 字段。
    /// </summary>
    private const string DashboardCurrentWaveSnapshotTableName = "dashboard_current_wave_snapshots";

    /// <summary>
    /// 存储 DashboardSnapshotStateTableName 字段。
    /// </summary>
    private const string DashboardSnapshotStateTableName = "dashboard_snapshot_states";

    /// <summary>
    /// 存储分表配置。
    /// </summary>
    private readonly ShardingOptions _shardingOptions;

    /// <summary>
    /// 初始化 HubDbContext。
    /// </summary>
    /// <param name="options">数据库上下文选项。</param>
    /// <param name="shardingOptions">分表配置。</param>
    public HubDbContext(DbContextOptions<HubDbContext> options, IOptions<ShardingOptions> shardingOptions)
        : base(options)
    {
        _shardingOptions = shardingOptions.Value;
    }

    /// <summary>
    /// 获取分拣任务轨迹集合。
    /// </summary>
    public DbSet<SortingTaskTraceEntity> SortingTaskTraces => Set<SortingTaskTraceEntity>();

    /// <summary>
    /// 获取业务任务集合。
    /// </summary>
    public DbSet<BusinessTaskEntity> BusinessTasks => Set<BusinessTaskEntity>();

    /// <summary>
    /// 获取扫描日志集合。
    /// </summary>
    public DbSet<ScanLogEntity> ScanLogs => Set<ScanLogEntity>();

    /// <summary>
    /// 获取落格日志集合。
    /// </summary>
    public DbSet<DropLogEntity> DropLogs => Set<DropLogEntity>();

    /// <summary>
    /// 获取同步批次集合。
    /// </summary>
    public DbSet<SyncBatchEntity> SyncBatches => Set<SyncBatchEntity>();

    /// <summary>
    /// 获取同步变更日志集合。
    /// </summary>
    public DbSet<SyncChangeLogEntity> SyncChangeLogs => Set<SyncChangeLogEntity>();

    /// <summary>
    /// 获取同步删除日志集合。
    /// </summary>
    public DbSet<SyncDeletionLogEntity> SyncDeletionLogs => Set<SyncDeletionLogEntity>();

    /// <summary>
    /// 获取波次清理敏感操作审计记录集合。
    /// </summary>
    public DbSet<WaveCleanupAuditLogEntity> WaveCleanupAuditLogs => Set<WaveCleanupAuditLogEntity>();

    /// <summary>
    /// 获取保留期清理自动执行审计记录集合。
    /// </summary>
    public DbSet<RetentionCleanupAuditLogEntity> RetentionCleanupAuditLogs => Set<RetentionCleanupAuditLogEntity>();

    /// <summary>
    /// 获取运行时租约集合。
    /// </summary>
    public DbSet<RuntimeLeaseEntity> RuntimeLeases => Set<RuntimeLeaseEntity>();

    /// <summary>
    /// 获取任务快照集合。
    /// </summary>
    public DbSet<DashboardTaskSnapshotEntity> DashboardTaskSnapshots => Set<DashboardTaskSnapshotEntity>();

    /// <summary>
    /// 获取扫描快照集合。
    /// </summary>
    public DbSet<DashboardScanSnapshotEntity> DashboardScanSnapshots => Set<DashboardScanSnapshotEntity>();

    /// <summary>
    /// 获取当前波次快照集合。
    /// </summary>
    public DbSet<DashboardCurrentWaveSnapshotEntity> DashboardCurrentWaveSnapshots => Set<DashboardCurrentWaveSnapshotEntity>();

    /// <summary>
    /// 获取快照状态集合。
    /// </summary>
    public DbSet<DashboardSnapshotStateEntity> DashboardSnapshotStates => Set<DashboardSnapshotStateEntity>();

    /// <summary>
    /// 构建实体映射。
    /// </summary>
    /// <param name="modelBuilder">模型构建器。</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 步骤：根据当前分表后缀映射分表实体，并为非分表实体注册固定表名映射。
        var suffix = TableSuffixScope.CurrentSuffix ?? string.Empty;
        var sortingTaskTraceTableName = $"{SortingTaskTraceLogicalTable}{suffix}";
        var businessTaskTableName = $"{BusinessTaskLogicalTable}{suffix}";
        var scanLogTableName = $"{ScanLogLogicalTable}{suffix}";
        var dropLogTableName = $"{DropLogLogicalTable}{suffix}";
        var syncBatchTableName = $"{SyncBatchLogicalTable}{suffix}";
        var syncChangeLogTableName = $"{SyncChangeLogLogicalTable}{suffix}";
        var syncDeletionLogTableName = $"{SyncDeletionLogLogicalTable}{suffix}";

        modelBuilder.ApplyConfiguration(new SortingTaskTraceEntityTypeConfiguration(sortingTaskTraceTableName, _shardingOptions.Schema));
        modelBuilder.ApplyConfiguration(new BusinessTaskEntityTypeConfiguration(businessTaskTableName, _shardingOptions.Schema));
        modelBuilder.ApplyConfiguration(new ScanLogEntityTypeConfiguration(scanLogTableName, _shardingOptions.Schema));
        modelBuilder.ApplyConfiguration(new DropLogEntityTypeConfiguration(dropLogTableName, _shardingOptions.Schema));
        modelBuilder.ApplyConfiguration(new SyncBatchEntityTypeConfiguration(syncBatchTableName, _shardingOptions.Schema));
        modelBuilder.ApplyConfiguration(new SyncChangeLogEntityTypeConfiguration(syncChangeLogTableName, _shardingOptions.Schema));
        modelBuilder.ApplyConfiguration(new SyncDeletionLogEntityTypeConfiguration(syncDeletionLogTableName, _shardingOptions.Schema));
        modelBuilder.ApplyConfiguration(new WaveCleanupAuditLogEntityTypeConfiguration(WaveCleanupAuditLogTableName, _shardingOptions.Schema));
        modelBuilder.ApplyConfiguration(new RetentionCleanupAuditLogEntityTypeConfiguration(RetentionCleanupAuditLogTableName, _shardingOptions.Schema));
        modelBuilder.ApplyConfiguration(new RuntimeLeaseEntityTypeConfiguration(RuntimeLeaseTableName, _shardingOptions.Schema));
        modelBuilder.ApplyConfiguration(new DashboardTaskSnapshotEntityTypeConfiguration(DashboardTaskSnapshotTableName, _shardingOptions.Schema));
        modelBuilder.ApplyConfiguration(new DashboardScanSnapshotEntityTypeConfiguration(DashboardScanSnapshotTableName, _shardingOptions.Schema));
        modelBuilder.ApplyConfiguration(new DashboardCurrentWaveSnapshotEntityTypeConfiguration(DashboardCurrentWaveSnapshotTableName, _shardingOptions.Schema));
        modelBuilder.ApplyConfiguration(new DashboardSnapshotStateEntityTypeConfiguration(DashboardSnapshotStateTableName, _shardingOptions.Schema));
    }
}
