using Microsoft.EntityFrameworkCore;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Configuration;
using EverydayChain.Hub.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using EverydayChain.Hub.Infrastructure.Services;
using EverydayChain.Hub.Application.Repositories;
using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using EverydayChain.Hub.Infrastructure.Repositories;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using System.Text.RegularExpressions;

namespace EverydayChain.Hub.Infrastructure.DependencyInjection;

/// <summary>
/// 基础设施层依赖注入扩展，统一向 DI 容器注册所有基础设施服务。
/// </summary>
public static class ServiceCollectionExtensions {
    /// <summary>安全标识符校验正则（仅允许字母、数字、下划线）。</summary>
    private static readonly Regex SqlIdentifierRegex = new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    /// <summary>
    /// 注册基础设施层全部服务，包括 EF Core 工厂、分表服务、调谐器、危险操作执行器与自动迁移托管服务。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configuration">应用配置，用于绑定 Sharding/AutoTune/DangerZone 配置节。</param>
    /// <returns>原服务集合（链式调用）。</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) {
        services.Configure<ShardingOptions>(options =>
        {
            configuration.GetSection(ShardingOptions.SectionName).Bind(options);
            var syncJobOptions = configuration.GetSection(SyncJobOptions.SectionName).Get<SyncJobOptions>() ?? new SyncJobOptions();
            options.ManagedLogicalTables = BuildManagedLogicalTables(options, syncJobOptions);
        });
        services.Configure<AutoTuneOptions>(configuration.GetSection(AutoTuneOptions.SectionName));
        services.Configure<DangerZoneOptions>(configuration.GetSection(DangerZoneOptions.SectionName));
        services.Configure<SyncJobOptions>(configuration.GetSection(SyncJobOptions.SectionName));
        services.Configure<RetentionJobOptions>(configuration.GetSection(RetentionJobOptions.SectionName));
        services.Configure<OracleOptions>(configuration.GetSection(OracleOptions.SectionName));

        var shardingOptions = configuration.GetSection(ShardingOptions.SectionName).Get<ShardingOptions>() ?? new ShardingOptions();
        var syncOptions = configuration.GetSection(SyncJobOptions.SectionName).Get<SyncJobOptions>() ?? new SyncJobOptions();
        shardingOptions.ManagedLogicalTables = BuildManagedLogicalTables(shardingOptions, syncOptions);

        services.AddDbContextFactory<HubDbContext>(options => {
            options.UseSqlServer(shardingOptions.ConnectionString);
            options.ReplaceService<IModelCacheKeyFactory, ShardModelCacheKeyFactory>();
        });

        services.AddSingleton<IShardSuffixResolver, MonthShardSuffixResolver>();
        services.AddSingleton<IDangerZoneExecutor, DangerZoneExecutor>();
        services.AddSingleton<IRuntimeStorageGuard, RuntimeStorageGuard>();
        services.AddSingleton<ISqlExecutionTuner, SqlExecutionTuner>();
        services.AddSingleton<IShardTableProvisioner, ShardTableProvisioner>();
        services.AddScoped<IAutoMigrationService, AutoMigrationService>();
        services.AddSingleton<ISortingTaskTraceWriter, SortingTaskTraceWriter>();
        services.AddSingleton<ISyncTaskConfigRepository, SyncTaskConfigRepository>();
        services.AddSingleton<IShardTableResolver, ShardTableResolver>();
        services.AddSingleton<IShardRetentionRepository, ShardRetentionRepository>();
        services.AddSingleton<IOracleSourceReader, OracleSourceReader>();
        services.AddSingleton<ISyncStagingRepository, SyncStagingRepository>();
        services.AddSingleton<ISyncUpsertRepository, SyncUpsertRepository>();
        services.AddSingleton<ISyncCheckpointRepository, SyncCheckpointRepository>();
        services.AddSingleton<ISyncBatchRepository, SyncBatchRepository>();
        services.AddSingleton<ISyncChangeLogRepository, SyncChangeLogRepository>();
        services.AddSingleton<ISyncDeletionRepository, SyncDeletionRepository>();
        services.AddSingleton<ISyncDeletionLogRepository, SyncDeletionLogRepository>();
        services.AddSingleton<ISyncWindowCalculator, SyncWindowCalculator>();
        services.AddSingleton<IDeletionExecutionService, DeletionExecutionService>();
        services.AddSingleton<IRetentionExecutionService, RetentionExecutionService>();
        services.AddSingleton<ISyncExecutionService, SyncExecutionService>();
        services.AddSingleton<ISyncOrchestrator, SyncOrchestrator>();
        services.AddHostedService<AutoMigrationHostedService>();

        return services;
    }

    /// <summary>
    /// 构建分表纳管逻辑表集合。
    /// </summary>
    /// <param name="shardingOptions">分表配置。</param>
    /// <param name="syncJobOptions">同步配置。</param>
    /// <returns>去重、去空白并通过安全校验后的逻辑表集合。</returns>
    /// <exception cref="InvalidOperationException">配置缺失或包含非法表名时抛出。</exception>
    private static HashSet<string> BuildManagedLogicalTables(ShardingOptions shardingOptions, SyncJobOptions syncJobOptions)
    {
        var managedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var table in shardingOptions.ManagedLogicalTables ?? [])
        {
            TryAddManagedTable(managedTables, table, "Sharding.ManagedLogicalTables");
        }

        foreach (var table in (syncJobOptions.Tables ?? []).Where(x => x.Enabled))
        {
            TryAddManagedTable(managedTables, table.TargetLogicalTable, $"SyncJob.Tables[{table.TableCode}].TargetLogicalTable");
        }

        if (managedTables.Count == 0)
        {
            TryAddManagedTable(managedTables, shardingOptions.BaseTableName, "Sharding.BaseTableName");
        }

        if (managedTables.Count == 0)
        {
            throw new InvalidOperationException("分表配置无效：Sharding.ManagedLogicalTables 与启用的 SyncJob.Tables.TargetLogicalTable 均为空，且 Sharding.BaseTableName 未配置。");
        }

        return managedTables;
    }

    /// <summary>
    /// 尝试将逻辑表名写入集合，并执行安全校验。
    /// </summary>
    /// <param name="managedTables">目标集合。</param>
    /// <param name="logicalTable">逻辑表名。</param>
    /// <param name="sourcePath">配置来源路径。</param>
    /// <exception cref="InvalidOperationException">表名不满足 SQL 标识符安全规则时抛出。</exception>
    private static void TryAddManagedTable(HashSet<string> managedTables, string? logicalTable, string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(logicalTable))
        {
            return;
        }

        var trimmed = logicalTable.Trim();
        if (!SqlIdentifierRegex.IsMatch(trimmed))
        {
            throw new InvalidOperationException($"分表配置无效：{sourcePath} 包含非法逻辑表名 '{trimmed}'，仅允许字母、数字、下划线。");
        }

        managedTables.Add(trimmed);
    }
}
