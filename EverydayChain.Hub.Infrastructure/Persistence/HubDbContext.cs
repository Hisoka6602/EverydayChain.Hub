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
    /// <summary>WMS 下发至亮灯拆零箱任务默认逻辑表名。</summary>
    private const string WmsSplitPickToLightCartonLogicalTable = "IDX_PICKTOLIGHT_CARTON1";
    /// <summary>WMS 下发至 WCS 分拣任务默认逻辑表名。</summary>
    private const string WmsPickToWcsLogicalTable = "IDX_PICKTOWCS2";

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

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var suffix = TableSuffixScope.CurrentSuffix ?? string.Empty;
        var sortingTaskTraceTableName = $"{SortingTaskTraceLogicalTable}{suffix}";
        var wmsSplitPickToLightCartonTableName = $"{WmsSplitPickToLightCartonLogicalTable}{suffix}";
        var wmsPickToWcsTableName = $"{WmsPickToWcsLogicalTable}{suffix}";

        modelBuilder.ApplyConfiguration(new SortingTaskTraceEntityTypeConfiguration(sortingTaskTraceTableName, _shardingOptions.Schema));
        ConfigureWmsSplitPickToLightCartonEntity(modelBuilder, wmsSplitPickToLightCartonTableName);
        ConfigureWmsPickToWcsEntity(modelBuilder, wmsPickToWcsTableName);
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
        builder.HasKey(x => x.CartonNo);
        builder.Property(x => x.CartonNo).IsRequired().HasMaxLength(30);
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
        builder.HasKey(x => x.UniqueId);
        builder.Property(x => x.UniqueId).IsRequired().HasMaxLength(30);
        builder.Property(x => x.LengthCm).HasColumnType("decimal(18,8)");
        builder.Property(x => x.WidthCm).HasColumnType("decimal(18,8)");
        builder.Property(x => x.HeightCm).HasColumnType("decimal(18,8)");
        builder.Property(x => x.VolumeCubicCm).HasColumnType("decimal(18,8)");
        builder.Property(x => x.GrossWeightGram).HasColumnType("decimal(18,8)");
    }
}
