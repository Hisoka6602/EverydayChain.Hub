using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Aggregates.DropLogAggregate;
using EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;
using EverydayChain.Hub.Domain.Aggregates.SyncBatchAggregate;
using EverydayChain.Hub.Domain.Aggregates.SyncChangeLogAggregate;
using EverydayChain.Hub.Domain.Aggregates.SyncDeletionLogAggregate;
using EverydayChain.Hub.Domain.Aggregates.SortingTaskTraceAggregate;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence.EntityConfigurations;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Persistence;

/// <summary>
/// 中台核心数据库上下文，根据当前分表后缀动态路由表名，
/// 配合 <see cref="ShardModelCacheKeyFactory"/> 保证不同后缀产生独立的 EF 模型缓存。
/// </summary>
public class HubDbContext : DbContext
{
    /// <summary>分拣任务追踪默认逻辑表名。</summary>
    private const string SortingTaskTraceLogicalTable = "sorting_task_trace";
    /// <summary>业务任务逻辑表名（分片表，按月后缀路由）。</summary>
    private const string BusinessTaskLogicalTable = "business_tasks";
    /// <summary>扫描日志逻辑表名（分片表，按月后缀路由）。</summary>
    private const string ScanLogLogicalTable = "scan_logs";
    /// <summary>落格日志逻辑表名（分片表，按月后缀路由）。</summary>
    private const string DropLogLogicalTable = "drop_logs";
    /// <summary>同步批次逻辑表名（分片表，按月后缀路由）。</summary>
    private const string SyncBatchLogicalTable = "sync_batches";
    /// <summary>同步变更日志逻辑表名（分片表，按月后缀路由）。</summary>
    private const string SyncChangeLogLogicalTable = "sync_change_logs";
    /// <summary>同步删除日志逻辑表名（分片表，按月后缀路由）。</summary>
    private const string SyncDeletionLogLogicalTable = "sync_deletion_logs";

    /// <summary>分表配置快照，用于动态拼接表名与 Schema。</summary>
    private readonly ShardingOptions _shardingOptions;

    /// <summary>
    /// 初始化 <see cref="HubDbContext"/>。
    /// </summary>
    /// <param name="options">EF Core 上下文选项。</param>
    /// <param name="shardingOptions">分表配置。</param>
    public HubDbContext(DbContextOptions<HubDbContext> options, IOptions<ShardingOptions> shardingOptions) : base(options)
    {
        _shardingOptions = shardingOptions.Value;
    }

    /// <summary>
    /// 分拣任务追踪实体集，实际映射到当前作用域对应的分表。
    /// </summary>
    public DbSet<SortingTaskTraceEntity> SortingTaskTraces => Set<SortingTaskTraceEntity>();
    /// <summary>
    /// 业务任务实体集，映射到按月分片表 <c>business_tasks_{yyyyMM}</c>。
    /// </summary>
    public DbSet<BusinessTaskEntity> BusinessTasks => Set<BusinessTaskEntity>();

    /// <summary>
    /// 扫描日志实体集，映射到按月分片表 <c>scan_logs_{yyyyMM}</c>。
    /// </summary>
    public DbSet<ScanLogEntity> ScanLogs => Set<ScanLogEntity>();

    /// <summary>
    /// 落格日志实体集，映射到按月分片表 <c>drop_logs_{yyyyMM}</c>。
    /// </summary>
    public DbSet<DropLogEntity> DropLogs => Set<DropLogEntity>();

    /// <summary>
    /// 同步批次实体集，映射到按月分片表 <c>sync_batches_{yyyyMM}</c>。
    /// </summary>
    public DbSet<SyncBatchEntity> SyncBatches => Set<SyncBatchEntity>();
    /// <summary>
    /// 同步变更日志实体集，映射到按月分片表 <c>sync_change_logs_{yyyyMM}</c>。
    /// </summary>
    public DbSet<SyncChangeLogEntity> SyncChangeLogs => Set<SyncChangeLogEntity>();
    /// <summary>
    /// 同步删除日志实体集，映射到按月分片表 <c>sync_deletion_logs_{yyyyMM}</c>。
    /// </summary>
    public DbSet<SyncDeletionLogEntity> SyncDeletionLogs => Set<SyncDeletionLogEntity>();

    /// <inheritdoc/>
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
    }
}
