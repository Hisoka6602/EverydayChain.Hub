using EverydayChain.Hub.Infrastructure.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EverydayChain.Hub.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) {
        services.Configure<ShardingOptions>(configuration.GetSection(ShardingOptions.SectionName));
        services.Configure<AutoTuneOptions>(configuration.GetSection(AutoTuneOptions.SectionName));

        var shardingOptions = configuration.GetSection(ShardingOptions.SectionName).Get<ShardingOptions>() ?? new ShardingOptions();

        services.AddDbContextFactory<HubDbContext>(options => {
            options.UseSqlServer(shardingOptions.ConnectionString);
            options.ReplaceService<IModelCacheKeyFactory, ShardModelCacheKeyFactory>();
        });

        services.AddSingleton<IShardSuffixResolver, MonthShardSuffixResolver>();
        services.AddSingleton<IDangerZoneExecutor, DangerZoneExecutor>();
        services.AddSingleton<ISqlExecutionTuner, SqlExecutionTuner>();
        services.AddSingleton<IShardTableManager, ShardTableManager>();
        services.AddScoped<IAutoMigrationService, AutoMigrationService>();
        services.AddScoped<ISortingTaskTraceWriter, SortingTaskTraceWriter>();
        services.AddHostedService<AutoMigrationHostedService>();

        return services;
    }
}
