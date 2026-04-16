using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Aggregates.DropLogAggregate;
using EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;
using EverydayChain.Hub.Domain.Aggregates.SyncBatchAggregate;
using EverydayChain.Hub.Domain.Aggregates.SortingTaskTraceAggregate;
using EverydayChain.Hub.Domain.Aggregates.WmsPickToWcsAggregate;
using EverydayChain.Hub.Domain.Aggregates.WmsSplitPickToLightCartonAggregate;
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
    /// <summary>WMS 下发至亮灯拆零箱任务默认逻辑表名（沿用遗留系统表命名）。</summary>
    private const string WmsSplitPickToLightCartonLogicalTable = "IDX_PICKTOLIGHT_CARTON1";
    /// <summary>WMS 下发至 WCS 分拣任务默认逻辑表名（沿用遗留系统表命名）。</summary>
    private const string WmsPickToWcsLogicalTable = "IDX_PICKTOWCS2";
    /// <summary>业务任务固定表名（非分片，不含后缀）。</summary>
    private const string BusinessTaskLogicalTable = "business_tasks";
    /// <summary>扫描日志固定表名（非分片，不含后缀）。</summary>
    private const string ScanLogLogicalTable = "scan_logs";
    /// <summary>落格日志固定表名（非分片，不含后缀）。</summary>
    private const string DropLogLogicalTable = "drop_logs";
    /// <summary>同步批次逻辑表名（分片表，按月后缀路由）。</summary>
    private const string SyncBatchLogicalTable = "sync_batches";

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
    /// WMS 下发至亮灯拆零箱任务实体集，实际映射到当前作用域对应的分表。
    /// </summary>
    public DbSet<WmsSplitPickToLightCartonEntity> WmsSplitPickToLightCartons => Set<WmsSplitPickToLightCartonEntity>();
    /// <summary>
    /// WMS 下发至 WCS 分拣任务实体集，实际映射到当前作用域对应的分表。
    /// </summary>
    public DbSet<WmsPickToWcsEntity> WmsPickToWcsTasks => Set<WmsPickToWcsEntity>();
    /// <summary>
    /// 业务任务实体集，映射到固定表 <c>business_tasks</c>（非分片）。
    /// </summary>
    public DbSet<BusinessTaskEntity> BusinessTasks => Set<BusinessTaskEntity>();

    /// <summary>
    /// 扫描日志实体集，映射到固定表 <c>scan_logs</c>（非分片）。
    /// </summary>
    public DbSet<ScanLogEntity> ScanLogs => Set<ScanLogEntity>();

    /// <summary>
    /// 落格日志实体集，映射到固定表 <c>drop_logs</c>（非分片）。
    /// </summary>
    public DbSet<DropLogEntity> DropLogs => Set<DropLogEntity>();

    /// <summary>
    /// 同步批次实体集，映射到按月分片表 <c>sync_batches_{yyyyMM}</c>。
    /// </summary>
    public DbSet<SyncBatchEntity> SyncBatches => Set<SyncBatchEntity>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var suffix = TableSuffixScope.CurrentSuffix ?? string.Empty;
        var sortingTaskTraceTableName = $"{SortingTaskTraceLogicalTable}{suffix}";
        var wmsSplitPickToLightCartonTableName = $"{WmsSplitPickToLightCartonLogicalTable}{suffix}";
        var wmsPickToWcsTableName = $"{WmsPickToWcsLogicalTable}{suffix}";
        var syncBatchTableName = $"{SyncBatchLogicalTable}{suffix}";

        modelBuilder.ApplyConfiguration(new SortingTaskTraceEntityTypeConfiguration(sortingTaskTraceTableName, _shardingOptions.Schema));
        ConfigureWmsSplitPickToLightCartonEntity(modelBuilder, wmsSplitPickToLightCartonTableName);
        ConfigureWmsPickToWcsEntity(modelBuilder, wmsPickToWcsTableName);
        // 业务任务使用固定表名，不随分片后缀变化。
        modelBuilder.ApplyConfiguration(new BusinessTaskEntityTypeConfiguration(BusinessTaskLogicalTable, _shardingOptions.Schema));
        // 扫描日志与落格日志使用固定表名，不随分片后缀变化。
        modelBuilder.ApplyConfiguration(new ScanLogEntityTypeConfiguration(ScanLogLogicalTable, _shardingOptions.Schema));
        modelBuilder.ApplyConfiguration(new DropLogEntityTypeConfiguration(DropLogLogicalTable, _shardingOptions.Schema));
        modelBuilder.ApplyConfiguration(new SyncBatchEntityTypeConfiguration(syncBatchTableName, _shardingOptions.Schema));
    }

    /// <summary>
    /// 配置 <see cref="WmsSplitPickToLightCartonEntity"/> 的动态分表映射。
    /// </summary>
    /// <param name="modelBuilder">模型构建器。</param>
    /// <param name="tableName">目标表名（已拼接后缀）。</param>
    private void ConfigureWmsSplitPickToLightCartonEntity(ModelBuilder modelBuilder, string tableName)
    {
        var builder = modelBuilder.Entity<WmsSplitPickToLightCartonEntity>();
        builder.ToTable(tableName, _shardingOptions.Schema);
        builder.HasKey(x => x.Id).IsClustered();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CartonNo).IsRequired().HasMaxLength(30);
        builder.HasIndex(x => x.CartonNo).IsUnique();
        builder.HasIndex(x => x.AddTime);
        builder.Property(x => x.LengthCm).HasColumnType("decimal(18,8)");
        builder.Property(x => x.WidthCm).HasColumnType("decimal(18,8)");
        builder.Property(x => x.HeightCm).HasColumnType("decimal(18,8)");
        builder.Property(x => x.VolumeCubicCm).HasColumnType("decimal(18,8)");
        builder.Property(x => x.GrossWeightGram).HasColumnType("decimal(18,8)");
    }

    /// <summary>
    /// 配置 <see cref="WmsPickToWcsEntity"/> 的动态分表映射。
    /// </summary>
    /// <param name="modelBuilder">模型构建器。</param>
    /// <param name="tableName">目标表名（已拼接后缀）。</param>
    private void ConfigureWmsPickToWcsEntity(ModelBuilder modelBuilder, string tableName)
    {
        var builder = modelBuilder.Entity<WmsPickToWcsEntity>();
        builder.ToTable(tableName, _shardingOptions.Schema);
        builder.HasKey(x => x.Id).IsClustered();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.UniqueId).HasMaxLength(30);
        builder.HasIndex(x => new { x.DocumentNo, x.AddTime });
        builder.HasIndex(x => x.UniqueId);
        builder.HasIndex(x => x.AddTime);
        builder.Property(x => x.MinUnitQuantity).HasColumnType("decimal(18,8)");
        builder.Property(x => x.LengthCm).HasColumnType("decimal(18,8)");
        builder.Property(x => x.WidthCm).HasColumnType("decimal(18,8)");
        builder.Property(x => x.HeightCm).HasColumnType("decimal(18,8)");
        builder.Property(x => x.VolumeCubicCm).HasColumnType("decimal(18,8)");
        builder.Property(x => x.GrossWeightGram).HasColumnType("decimal(18,8)");
    }
}
