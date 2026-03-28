using EverydayChain.Hub.Domain.Aggregates.SortingTaskTraceAggregate;
using EverydayChain.Hub.Infrastructure.Options;
using EverydayChain.Hub.Infrastructure.Persistence.EntityConfigurations;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Persistence;

public class HubDbContext : DbContext {
    private readonly ShardingOptions _shardingOptions;

    public HubDbContext(DbContextOptions<HubDbContext> options, IOptions<ShardingOptions> shardingOptions) : base(options) {
        _shardingOptions = shardingOptions.Value;
    }

    public DbSet<SortingTaskTraceEntity> SortingTaskTraces => Set<SortingTaskTraceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        var suffix = TableSuffixScope.CurrentSuffix ?? string.Empty;
        var tableName = $"{_shardingOptions.BaseTableName}{suffix}";
        modelBuilder.ApplyConfiguration(new SortingTaskTraceEntityTypeConfiguration(tableName, _shardingOptions.Schema));
    }
}
