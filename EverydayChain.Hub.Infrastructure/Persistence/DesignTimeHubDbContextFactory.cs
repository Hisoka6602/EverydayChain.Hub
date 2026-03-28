using EverydayChain.Hub.Infrastructure.Options;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Persistence;

public class DesignTimeHubDbContextFactory : IDesignTimeDbContextFactory<HubDbContext> {
    public HubDbContext CreateDbContext(string[] args) {
        var optionsBuilder = new DbContextOptionsBuilder<HubDbContext>();
        var shardingOptions = new ShardingOptions();
        optionsBuilder.UseSqlServer(shardingOptions.ConnectionString);
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, ShardModelCacheKeyFactory>();

        using var _ = TableSuffixScope.Use(string.Empty);
        return new HubDbContext(optionsBuilder.Options, Options.Create(shardingOptions));
    }
}
