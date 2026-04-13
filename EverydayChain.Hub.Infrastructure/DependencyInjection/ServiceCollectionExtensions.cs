using Microsoft.EntityFrameworkCore;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Configuration;
using EverydayChain.Hub.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using EverydayChain.Hub.SharedKernel.Utilities;
using EverydayChain.Hub.Infrastructure.Services;
using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using EverydayChain.Hub.Infrastructure.Repositories;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Abstractions.Sync;
using EverydayChain.Hub.Application.ScanMatch.Services;
using EverydayChain.Hub.Application.TaskExecution.Services;
using EverydayChain.Hub.Infrastructure.Sync.Readers;
using EverydayChain.Hub.Infrastructure.Sync.Services;
using EverydayChain.Hub.Infrastructure.Sync.Writers;

namespace EverydayChain.Hub.Infrastructure.DependencyInjection;

/// <summary>
/// 基础设施层依赖注入扩展，统一向 DI 容器注册所有基础设施服务。
/// </summary>
public static class ServiceCollectionExtensions {
    /// <summary>分拣任务追踪逻辑表名。</summary>
    private const string SortingTaskTraceLogicalTable = "sorting_task_trace";

    /// <summary>
    /// 注册基础设施层全部服务，包括 EF Core 工厂、分表服务、调谐器、危险操作执行器与自动迁移应用服务（不含 HostedService 注册，该注册由 Host 层 Program.cs 负责）。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configuration">应用配置，用于绑定 Sharding/AutoTune/DangerZone 配置节。</param>
    /// <returns>原服务集合（链式调用）。</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) {
        services.Configure<ShardingOptions>(configuration.GetSection(ShardingOptions.SectionName));
        services.Configure<AutoTuneOptions>(configuration.GetSection(AutoTuneOptions.SectionName));
        services.Configure<DangerZoneOptions>(configuration.GetSection(DangerZoneOptions.SectionName));
        services.Configure<SyncJobOptions>(configuration.GetSection(SyncJobOptions.SectionName));
        services.Configure<RetentionJobOptions>(configuration.GetSection(RetentionJobOptions.SectionName));
        services.Configure<OracleOptions>(configuration.GetSection(OracleOptions.SectionName));

        var shardingOptions = configuration.GetSection(ShardingOptions.SectionName).Get<ShardingOptions>() ?? new ShardingOptions();
        var syncOptions = configuration.GetSection(SyncJobOptions.SectionName).Get<SyncJobOptions>() ?? new SyncJobOptions();
        var managedLogicalTables = BuildManagedLogicalTables(syncOptions).ToArray();

        services.AddDbContextFactory<HubDbContext>(options => {
            options.UseSqlServer(shardingOptions.ConnectionString, sqlServerOptions => {
                sqlServerOptions.EnableRetryOnFailure();
            });
            options.ReplaceService<IModelCacheKeyFactory, ShardModelCacheKeyFactory>();
        });
        services.AddSingleton<IShardSuffixResolver, MonthShardSuffixResolver>();
        services.AddSingleton<IDangerZoneExecutor, DangerZoneExecutor>();
        services.AddSingleton<IRuntimeStorageGuard, RuntimeStorageGuard>();
        services.AddSingleton<ISqlExecutionTuner, SqlExecutionTuner>();
        services.AddSingleton<IReadOnlyList<string>>(managedLogicalTables);
        services.AddSingleton<IShardTableProvisioner, ShardTableProvisioner>();
        services.AddScoped<IAutoMigrationService, AutoMigrationService>();
        services.AddSingleton<ISortingTaskTraceWriter, SortingTaskTraceWriter>();
        services.AddSingleton<ISyncTaskConfigRepository, SyncTaskConfigRepository>();
        services.AddSingleton<IShardTableResolver, ShardTableResolver>();
        services.AddSingleton<IShardRetentionRepository, ShardRetentionRepository>();
        services.AddSingleton<IOracleSourceReader, OracleSourceReader>();
        services.AddSingleton<ISyncStagingRepository, SyncStagingRepository>();
        services.AddSingleton<ISyncUpsertRepository, SqlServerSyncUpsertRepository>();
        services.AddSingleton<ISyncCheckpointRepository, SyncCheckpointRepository>();
        services.AddSingleton<ISyncBatchRepository, InMemorySyncBatchRepository>();
        services.AddSingleton<ISyncChangeLogRepository, InMemorySyncChangeLogRepository>();
        services.AddSingleton<ISyncDeletionRepository, SyncDeletionRepository>();
        services.AddSingleton<ISyncDeletionLogRepository, InMemorySyncDeletionLogRepository>();
        services.AddSingleton<IOracleStatusDrivenSourceReader, OracleStatusDrivenSourceReader>();
        services.AddSingleton<ISqlServerAppendOnlyWriter, SqlServerAppendOnlyWriter>();
        services.AddSingleton<IOracleRemoteStatusWriter, OracleRemoteStatusWriter>();
        services.AddSingleton<IRemoteStatusConsumeService, RemoteStatusConsumeService>();
        services.AddSingleton<ISyncWindowCalculator, SyncWindowCalculator>();
        services.AddSingleton<IBusinessTaskMaterializer, BusinessTaskMaterializer>();
        services.AddSingleton<IBarcodeParser, BarcodeParser>();
        services.AddSingleton<IBusinessTaskRepository, BusinessTaskRepository>();
        services.AddSingleton<IScanMatchService, ScanMatchService>();
        services.AddSingleton<ITaskExecutionService, TaskExecutionService>();
        // 注册 API 骨架服务：扫描上传、请求格口、落格回传（PR-05/06/07 接入真实实现）。
        services.AddSingleton<IScanIngressService, ScanIngressService>();
        services.AddSingleton<IChuteQueryService, ChuteQueryService>();
        services.AddSingleton<IDropFeedbackService, DropFeedbackService>();
        services.AddSingleton<IDeletionExecutionService, DeletionExecutionService>();
        services.AddSingleton<IRetentionExecutionService, RetentionExecutionService>();
        services.AddSingleton<ISyncExecutionService, SyncExecutionService>();
        services.AddSingleton<ISyncOrchestrator, SyncOrchestrator>();

        return services;
    }

    /// <summary>
    /// 构建分表纳管逻辑表集合。
    /// </summary>
    /// <param name="syncJobOptions">同步配置。</param>
    /// <returns>去重、去空白并通过安全校验后的逻辑表集合（大小写不敏感去重，保留首次出现的原始大小写）。</returns>
    /// <exception cref="InvalidOperationException">配置缺失或包含非法表名时抛出。</exception>
    public static HashSet<string> BuildManagedLogicalTables(SyncJobOptions syncJobOptions) {
        var managedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        LogicalTableNameNormalizer.AddValidated(managedTables, SortingTaskTraceLogicalTable, "Sharding.SortingTaskTrace");
        foreach (var table in (syncJobOptions.Tables ?? []).Where(x => x.Enabled)) {
            if (string.IsNullOrWhiteSpace(table.TargetLogicalTable)) {
                throw new InvalidOperationException($"分表配置无效：启用表 {table.TableCode} 的 TargetLogicalTable 不能为空白。");
            }

            LogicalTableNameNormalizer.AddValidated(managedTables, table.TargetLogicalTable, $"SyncJob.Tables[{table.TableCode}].TargetLogicalTable");
        }

        return managedTables;
    }
}
