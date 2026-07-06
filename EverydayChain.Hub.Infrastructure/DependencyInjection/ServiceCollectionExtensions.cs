using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Configuration;
using EverydayChain.Hub.Application.Services;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.Extensions.DependencyInjection;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;
using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using EverydayChain.Hub.Infrastructure.Integrations;
using EverydayChain.Hub.Infrastructure.Repositories;
using EverydayChain.Hub.Infrastructure.Sync.Readers;
using EverydayChain.Hub.Infrastructure.Sync.Writers;
using EverydayChain.Hub.Infrastructure.Sync.Services;
using EverydayChain.Hub.Application.Abstractions.Sync;
using EverydayChain.Hub.Application.Feedback.Services;
using EverydayChain.Hub.Application.ScanMatch.Services;
using EverydayChain.Hub.Application.MultiLabel.Services;
using EverydayChain.Hub.Application.WaveCleanup.Services;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Recirculation.Services;
using EverydayChain.Hub.Application.TaskExecution.Services;
using EverydayChain.Hub.Application.MultiLabel.Abstractions;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.WaveCleanup.Abstractions;
using EverydayChain.Hub.Application.Abstractions.Integrations;
using EverydayChain.Hub.Application.Recirculation.Abstractions;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Abstractions.Infrastructure;
using EverydayChain.Hub.Application.Queries;
using EverydayChain.Hub.Infrastructure.Services.Sharding;

namespace EverydayChain.Hub.Infrastructure.DependencyInjection;

/// <summary>
/// 定义当前类型。
/// </summary>
public static class ServiceCollectionExtensions {

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string SortingTaskTraceLogicalTable = "sorting_task_trace";

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string SyncBatchLogicalTable = "sync_batches";

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string BusinessTaskLogicalTable = "business_tasks";

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string ScanLogLogicalTable = "scan_logs";

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string DropLogLogicalTable = "drop_logs";
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string SyncChangeLogLogicalTable = "sync_change_logs";
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string SyncDeletionLogLogicalTable = "sync_deletion_logs";

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) {
        // 步骤：按既定流程执行当前方法逻辑。
        services.Configure<ShardingOptions>(configuration.GetSection(ShardingOptions.SectionName));
        services.Configure<AutoTuneOptions>(configuration.GetSection(AutoTuneOptions.SectionName));
        services.Configure<DangerZoneOptions>(configuration.GetSection(DangerZoneOptions.SectionName));
        services.Configure<SyncJobOptions>(configuration.GetSection(SyncJobOptions.SectionName));
        services.Configure<RetentionJobOptions>(configuration.GetSection(RetentionJobOptions.SectionName));
        services.Configure<OracleOptions>(configuration.GetSection(OracleOptions.SectionName));
        services.Configure<WmsFeedbackOptions>(configuration.GetSection(WmsFeedbackOptions.SectionName));
        services.Configure<FeedbackCompensationJobOptions>(configuration.GetSection(FeedbackCompensationJobOptions.SectionName));
        services.Configure<ExceptionRuleOptions>(configuration.GetSection(ExceptionRuleOptions.SectionName));
        services.Configure<QueryCacheOptions>(configuration.GetSection(QueryCacheOptions.SectionName));
        services.Configure<EfCoreOptions>(configuration.GetSection(EfCoreOptions.SectionName));
        services.Configure<DashboardSnapshotOptions>(configuration.GetSection(DashboardSnapshotOptions.SectionName));

        var shardingOptions = configuration.GetSection(ShardingOptions.SectionName).Get<ShardingOptions>() ?? new ShardingOptions();
        var queryCacheOptions = configuration.GetSection(QueryCacheOptions.SectionName).Get<QueryCacheOptions>() ?? new QueryCacheOptions();
        var syncOptions = configuration.GetSection(SyncJobOptions.SectionName).Get<SyncJobOptions>() ?? new SyncJobOptions();
        var retentionJobOptions = configuration.GetSection(RetentionJobOptions.SectionName).Get<RetentionJobOptions>() ?? new RetentionJobOptions();
        var efCoreOptions = configuration.GetSection(EfCoreOptions.SectionName).Get<EfCoreOptions>() ?? new EfCoreOptions();
        var managedLogicalTables = BuildManagedLogicalTables(syncOptions).ToArray();
        var commandTimeoutSeconds = Math.Clamp(efCoreOptions.CommandTimeoutSeconds > 0 ? efCoreOptions.CommandTimeoutSeconds : 30, 1, 600);

        services.AddMemoryCache();
        services.AddDbContextFactory<HubDbContext>(options => {
            options.UseSqlServer(shardingOptions.ConnectionString, sqlServerOptions => {
                sqlServerOptions.EnableRetryOnFailure();
                sqlServerOptions.CommandTimeout(commandTimeoutSeconds);
            });
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            options.ReplaceService<IModelCacheKeyFactory, ShardModelCacheKeyFactory>();
        });
        services.AddSingleton<IShardSuffixResolver, MonthShardSuffixResolver>();
        services.AddSingleton<IDangerZoneExecutor, DangerZoneExecutor>();
        services.AddSingleton<IRuntimeStorageGuard, RuntimeStorageGuard>();
        services.AddSingleton<ISqlExecutionTuner, SqlExecutionTuner>();
        services.AddSingleton<IReadOnlyList<string>>(managedLogicalTables);
        services.AddSingleton<IReadOnlyList<RetentionLogTableOptions>>(retentionJobOptions.LogTables);
        services.AddSingleton<IShardTableProvisioner, ShardTableProvisioner>();
        services.AddSingleton<IShardSchemaSynchronizer, ShardSchemaSynchronizer>();
        services.AddScoped<IAutoMigrationService, AutoMigrationService>();
        services.AddSingleton<IDashboardSnapshotService, DashboardSnapshotService>();
        services.AddSingleton<ISortingTaskTraceWriter, SortingTaskTraceWriter>();
        services.AddSingleton<ISyncTaskConfigRepository, SyncTaskConfigRepository>();
        services.AddSingleton<IShardTableResolver, ShardTableResolver>();
        services.AddSingleton<IShardRetentionRepository, ShardRetentionRepository>();
        services.AddSingleton<IRuntimeLeaseRepository, RuntimeLeaseRepository>();
        services.AddSingleton<IOracleSourceReader, OracleSourceReader>();
        services.AddSingleton<ISyncStagingRepository, SyncStagingRepository>();
        services.AddSingleton<ISyncUpsertRepository, SqlServerSyncUpsertRepository>();
        services.AddSingleton<ISyncCheckpointRepository, SyncCheckpointRepository>();
        services.AddSingleton<ISyncBatchRepository, SyncBatchRepository>();
        services.AddSingleton<ISyncChangeLogRepository, SyncChangeLogRepository>();
        services.AddSingleton<ISyncDeletionRepository, SyncDeletionRepository>();
        services.AddSingleton<ISyncDeletionLogRepository, SyncDeletionLogRepository>();
        services.AddSingleton<IOracleStatusDrivenSourceReader, OracleStatusDrivenSourceReader>();
        services.AddSingleton<IOracleRemoteStatusWriter, OracleRemoteStatusWriter>();
        services.AddSingleton<IBusinessTaskStatusConsumeService, BusinessTaskStatusConsumeService>();
        services.AddSingleton<ISyncWindowCalculator, SyncWindowCalculator>();
        services.AddSingleton<IBusinessTaskMaterializer, BusinessTaskMaterializer>();
        services.AddSingleton<IBusinessTaskProjectionService, BusinessTaskProjectionService>();
        services.AddSingleton<IBusinessTaskProjectionBackfillService, BusinessTaskProjectionBackfillService>();
        services.AddSingleton<IBarcodeParser, BarcodeParser>();
        services.AddSingleton<IBusinessTaskRepository, BusinessTaskRepository>();
        services.AddSingleton<IBusinessTaskSeedRepository, BusinessTaskSeedRepository>();
        services.AddSingleton<IScanMatchService, ScanMatchService>();
        services.AddSingleton<ITaskExecutionService, TaskExecutionService>();
        services.AddSingleton<IScanLogRepository, ScanLogRepository>();
        services.AddSingleton<IDropLogRepository, DropLogRepository>();
        services.AddSingleton<IScanIngressService, ScanIngressService>();
        services.AddSingleton<IChuteQueryService, ChuteQueryService>();
        services.AddSingleton<IDropFeedbackService, DropFeedbackService>();
        services.AddSingleton<IApiWarmupService, ApiWarmupService>();
        services.AddSingleton<IBusinessTaskSeedService, BusinessTaskSeedService>();
        services.AddSingleton<IGlobalDashboardQueryService>(sp =>
            /// <summary>
            /// 执行当前方法。
            /// </summary>
            new GlobalDashboardQueryService(
                sp.GetRequiredService<IBusinessTaskRepository>(),
                sp.GetRequiredService<IScanLogRepository>(),
                sp.GetRequiredService<ISyncBatchRepository>(),
                sp.GetRequiredService<ISyncTaskConfigRepository>(),
                sp.GetRequiredService<IMemoryCache>(),
                queryCacheOptions));
        services.AddSingleton<IDockDashboardQueryService>(sp =>
            /// <summary>
            /// 执行当前方法。
            /// </summary>
            new DockDashboardQueryService(
                sp.GetRequiredService<IBusinessTaskRepository>(),
                sp.GetRequiredService<IMemoryCache>(),
                queryCacheOptions));
        services.AddSingleton<ISortingReportQueryService>(sp =>
            /// <summary>
            /// 执行当前方法。
            /// </summary>
            new SortingReportQueryService(
                sp.GetRequiredService<IBusinessTaskRepository>(),
                sp.GetRequiredService<IMemoryCache>(),
                queryCacheOptions));
        services.AddSingleton<IBoxTrackingQueryService, BoxTrackingQueryService>();
        services.AddSingleton<IWaveQueryService>(sp =>
            /// <summary>
            /// 执行当前方法。
            /// </summary>
            new WaveQueryService(
                sp.GetRequiredService<IBusinessTaskRepository>(),
                sp.GetRequiredService<ILogger<WaveQueryService>>(),
                sp.GetRequiredService<IMemoryCache>(),
                queryCacheOptions));
        services.AddSingleton<IRecirculationQueryService>(sp =>
            /// <summary>
            /// 执行当前方法。
            /// </summary>
            new RecirculationQueryService(
                sp.GetRequiredService<IBusinessTaskRepository>(),
                sp.GetRequiredService<IMemoryCache>(),
                queryCacheOptions));
        services.AddSingleton<IExportCatalogQueryService, ExportCatalogQueryService>();
        services.AddSingleton<IBusinessTaskReadService, BusinessTaskReadService>();
        services.AddSingleton<IDeletionExecutionService, DeletionExecutionService>();
        services.AddSingleton<IRetentionExecutionService, RetentionExecutionService>();
        services.AddSingleton<ISyncExecutionService, SyncExecutionService>();
        services.AddSingleton<ISyncOrchestrator, SyncOrchestrator>();
        services.AddSingleton<IWmsOracleFeedbackGateway, OracleWmsFeedbackGateway>();
        services.AddSingleton<IWmsFeedbackService>(sp =>
            /// <summary>
            /// 执行当前方法。
            /// </summary>
            new WmsFeedbackService(
                sp.GetRequiredService<IBusinessTaskRepository>(),
                sp.GetRequiredService<IWmsOracleFeedbackGateway>(),
                sp.GetRequiredService<IOptions<WmsFeedbackOptions>>().Value,
                sp.GetRequiredService<ILogger<WmsFeedbackService>>()));
        services.AddSingleton<IFeedbackCompensationService, FeedbackCompensationService>();
        services.AddSingleton<IWaveCleanupService>(sp =>
            /// <summary>
            /// 执行当前方法。
            /// </summary>
            new WaveCleanupService(
                sp.GetRequiredService<IBusinessTaskRepository>(),
                sp.GetRequiredService<IOptions<ExceptionRuleOptions>>().Value,
                sp.GetRequiredService<ILogger<WaveCleanupService>>()));
        services.AddSingleton<IMultiLabelDecisionService>(sp =>
            /// <summary>
            /// 执行当前方法。
            /// </summary>
            new MultiLabelDecisionService(
                sp.GetRequiredService<IBusinessTaskRepository>(),
                sp.GetRequiredService<IOptions<ExceptionRuleOptions>>().Value,
                sp.GetRequiredService<ILogger<MultiLabelDecisionService>>()));
        services.AddSingleton<IRecirculationService>(sp =>
            /// <summary>
            /// 执行当前方法。
            /// </summary>
            new RecirculationService(
                sp.GetRequiredService<IBusinessTaskRepository>(),
                sp.GetRequiredService<IOptions<ExceptionRuleOptions>>().Value,
                sp.GetRequiredService<ILogger<RecirculationService>>()));

        return services;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public static HashSet<string> BuildManagedLogicalTables(SyncJobOptions syncJobOptions) {
        // 步骤：按既定流程执行当前方法逻辑。
        var managedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        LogicalTableNameNormalizer.AddValidated(managedTables, SortingTaskTraceLogicalTable, "Sharding.SortingTaskTrace");
        LogicalTableNameNormalizer.AddValidated(managedTables, SyncBatchLogicalTable, "Sharding.SyncBatch");
        LogicalTableNameNormalizer.AddValidated(managedTables, BusinessTaskLogicalTable, "Sharding.BusinessTask");
        LogicalTableNameNormalizer.AddValidated(managedTables, ScanLogLogicalTable, "Sharding.ScanLog");
        LogicalTableNameNormalizer.AddValidated(managedTables, DropLogLogicalTable, "Sharding.DropLog");
        LogicalTableNameNormalizer.AddValidated(managedTables, SyncChangeLogLogicalTable, "Sharding.SyncChangeLog");
        LogicalTableNameNormalizer.AddValidated(managedTables, SyncDeletionLogLogicalTable, "Sharding.SyncDeletionLog");
        foreach (var table in (syncJobOptions.Tables ?? []).Where(x => x.Enabled)) {
            if (string.IsNullOrWhiteSpace(table.TargetLogicalTable)) {
                throw new InvalidOperationException($"分表配置无效：启用表 {table.TableCode} 的 TargetLogicalTable 不能为空白。");
            }

            LogicalTableNameNormalizer.AddValidated(managedTables, table.TargetLogicalTable, $"SyncJob.Tables[{table.TableCode}].TargetLogicalTable");
        }

        return managedTables;
    }

}

