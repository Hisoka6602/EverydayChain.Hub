using EverydayChain.Hub.Infrastructure.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EverydayChain.Hub.Infrastructure.DependencyInjection;

/// <summary>
/// 基础设施层依赖注入扩展，统一向 DI 容器注册所有基础设施服务。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册基础设施层全部服务，包括 EF Core 工厂、分表服务、调谐器与自动迁移托管服务。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configuration">应用配置，用于绑定 Sharding/AutoTune 配置节。</param>
    /// <returns>原服务集合（链式调用）。</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ShardingOptions>(configuration.GetSection(ShardingOptions.SectionName));
        services.Configure<AutoTuneOptions>(configuration.GetSection(AutoTuneOptions.SectionName));

        var shardingOptions = configuration.GetSection(ShardingOptions.SectionName).Get<ShardingOptions>() ?? new ShardingOptions();

        services.AddDbContextFactory<HubDbContext>(options =>
        {
            options.UseSqlServer(shardingOptions.ConnectionString);
            options.ReplaceService<IModelCacheKeyFactory, ShardModelCacheKeyFactory>();
        });

        services.AddSingleton<IShardSuffixResolver, MonthShardSuffixResolver>();
        services.AddSingleton<IDangerZoneExecutor, DangerZoneExecutor>();
        services.AddSingleton<ISqlExecutionTuner, SqlExecutionTuner>();
        services.AddSingleton<IShardTableProvisioner, ShardTableProvisioner>();
        services.AddScoped<IAutoMigrationService, AutoMigrationService>();
        services.AddScoped<ISortingTaskTraceWriter, SortingTaskTraceWriter>();
        services.AddHostedService<AutoMigrationHostedService>();

        return services;
    }
}
