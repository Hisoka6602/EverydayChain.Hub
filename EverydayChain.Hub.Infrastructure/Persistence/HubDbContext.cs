using EverydayChain.Hub.Domain.Aggregates.SortingTaskTraceAggregate;
using EverydayChain.Hub.Infrastructure.Options;
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

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var suffix = TableSuffixScope.CurrentSuffix ?? string.Empty;
        var tableName = $"{_shardingOptions.BaseTableName}{suffix}";
        modelBuilder.ApplyConfiguration(new SortingTaskTraceEntityTypeConfiguration(tableName, _shardingOptions.Schema));
    }
}
