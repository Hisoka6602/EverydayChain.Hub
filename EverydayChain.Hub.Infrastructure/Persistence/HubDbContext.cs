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
/// 定义当前类型。
/// </summary>
public sealed class HubDbContext : DbContext
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string SortingTaskTraceLogicalTable = "sorting_task_trace";
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string BusinessTaskLogicalTable = "business_tasks";
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string ScanLogLogicalTable = "scan_logs";
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string DropLogLogicalTable = "drop_logs";
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string SyncBatchLogicalTable = "sync_batches";
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string SyncChangeLogLogicalTable = "sync_change_logs";
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string SyncDeletionLogLogicalTable = "sync_deletion_logs";
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string RuntimeLeaseTableName = "runtime_leases";
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string DashboardTaskSnapshotTableName = "dashboard_task_snapshots";
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string DashboardScanSnapshotTableName = "dashboard_scan_snapshots";
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string DashboardCurrentWaveSnapshotTableName = "dashboard_current_wave_snapshots";
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string DashboardSnapshotStateTableName = "dashboard_snapshot_states";

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly ShardingOptions _shardingOptions;

    public HubDbContext(DbContextOptions<HubDbContext> options, IOptions<ShardingOptions> shardingOptions)
        : base(options)
    {
        _shardingOptions = shardingOptions.Value;
    }

    public DbSet<SortingTaskTraceEntity> SortingTaskTraces => Set<SortingTaskTraceEntity>();

    public DbSet<BusinessTaskEntity> BusinessTasks => Set<BusinessTaskEntity>();

    public DbSet<ScanLogEntity> ScanLogs => Set<ScanLogEntity>();

    public DbSet<DropLogEntity> DropLogs => Set<DropLogEntity>();

    public DbSet<SyncBatchEntity> SyncBatches => Set<SyncBatchEntity>();

    public DbSet<SyncChangeLogEntity> SyncChangeLogs => Set<SyncChangeLogEntity>();

    public DbSet<SyncDeletionLogEntity> SyncDeletionLogs => Set<SyncDeletionLogEntity>();

    public DbSet<RuntimeLeaseEntity> RuntimeLeases => Set<RuntimeLeaseEntity>();

    public DbSet<DashboardTaskSnapshotEntity> DashboardTaskSnapshots => Set<DashboardTaskSnapshotEntity>();

    public DbSet<DashboardScanSnapshotEntity> DashboardScanSnapshots => Set<DashboardScanSnapshotEntity>();

    public DbSet<DashboardCurrentWaveSnapshotEntity> DashboardCurrentWaveSnapshots => Set<DashboardCurrentWaveSnapshotEntity>();

    public DbSet<DashboardSnapshotStateEntity> DashboardSnapshotStates => Set<DashboardSnapshotStateEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
        modelBuilder.ApplyConfiguration(new RuntimeLeaseEntityTypeConfiguration(RuntimeLeaseTableName, _shardingOptions.Schema));
        modelBuilder.ApplyConfiguration(new DashboardTaskSnapshotEntityTypeConfiguration(DashboardTaskSnapshotTableName, _shardingOptions.Schema));
        modelBuilder.ApplyConfiguration(new DashboardScanSnapshotEntityTypeConfiguration(DashboardScanSnapshotTableName, _shardingOptions.Schema));
        modelBuilder.ApplyConfiguration(new DashboardCurrentWaveSnapshotEntityTypeConfiguration(DashboardCurrentWaveSnapshotTableName, _shardingOptions.Schema));
        modelBuilder.ApplyConfiguration(new DashboardSnapshotStateEntityTypeConfiguration(DashboardSnapshotStateTableName, _shardingOptions.Schema));
    }
}

