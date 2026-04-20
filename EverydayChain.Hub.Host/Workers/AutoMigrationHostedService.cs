using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Services;

namespace EverydayChain.Hub.Host.Workers;

/// <summary>
/// 托管服务入口，在应用启动阶段触发自动迁移与分表预置流程。
/// </summary>
public class AutoMigrationHostedService(
    IServiceScopeFactory scopeFactory,
    IRuntimeStorageGuard runtimeStorageGuard,
    ILogger<AutoMigrationHostedService> logger) : IHostedService {

    /// <summary>自动迁移阶段标识。</summary>
    private const string AutoMigrationStage = "自动迁移阶段";
    /// <summary>启动自检与自动迁移单阶段超时秒数。</summary>
    private const int StartupStageTimeoutSeconds = 120;
    /// <summary>启动预热总超时秒数。</summary>
    private const int WarmupTimeoutSeconds = 90;
    /// <summary>波次查询预热占位波次编码。</summary>
    private const string WarmupWaveCode = "WARMUP";
    /// <summary>条码查询预热占位文本。</summary>
    private const string WarmupBarcode = "WARMUP-BARCODE";
    /// <summary>任务号查询预热占位文本。</summary>
    private const string WarmupTaskCode = "WARMUP-TASK";
    /// <summary>来源表查询预热占位文本。</summary>
    private const string WarmupSourceTableCode = "WARMUP_SOURCE";
    /// <summary>业务键查询预热占位文本。</summary>
    private const string WarmupBusinessKey = "WARMUP_KEY";

    /// <summary>
    /// 应用启动时调用，创建作用域并执行 <see cref="IAutoMigrationService.RunAsync"/>。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task StartAsync(CancellationToken cancellationToken) {
        var currentStage = "启动初始化阶段";
        try {
            logger.LogInformation("启动自动迁移与分表自治流程。");
            currentStage = "启动自检阶段";
            using var healthCheckCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            healthCheckCts.CancelAfter(TimeSpan.FromSeconds(StartupStageTimeoutSeconds));
            await runtimeStorageGuard.EnsureStartupHealthyAsync(healthCheckCts.Token);
            using var scope = scopeFactory.CreateScope();
            var autoMigrationService = scope.ServiceProvider.GetRequiredService<IAutoMigrationService>();
            currentStage = AutoMigrationStage;
            using var migrationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            migrationCts.CancelAfter(TimeSpan.FromSeconds(StartupStageTimeoutSeconds));
            await autoMigrationService.RunAsync(migrationCts.Token);
            StartApiWarmupInBackground();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            logger.LogError(
                "自动迁移与分表自治流程在{Stage}执行超时（>{TimeoutSeconds}s），已降级跳过并继续启动。",
                currentStage,
                StartupStageTimeoutSeconds);
            return;
        }
        catch (Exception ex) when (string.Equals(currentStage, AutoMigrationStage, StringComparison.Ordinal) && IsDatabaseConnectivityException(ex)) {
            if (TryGetSqlException(ex) is SqlException sqlException) {
                logger.LogError(
                    ex,
                    "自动迁移阶段命中数据库连接类异常降级。ConnectivityDegraded=true, SqlErrors={SqlErrors}, ClientConnectionId={ClientConnectionId}。已跳过自动迁移并继续启动。",
                    BuildSqlErrorNumbers(sqlException),
                    sqlException.ClientConnectionId);
            }
            else {
                logger.LogError(ex, "自动迁移阶段命中数据库连接类异常降级。ConnectivityDegraded=true。已跳过自动迁移并继续启动。");
            }
            return;
        }
        catch (Exception ex) {
            logger.LogError(ex, "自动迁移与分表自治流程在{Stage}发生异常，应用启动终止。", currentStage);
            throw;
        }
    }

    /// <summary>
    /// 应用停止时调用，无需执行任何清理。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// 在后台触发 API 预热，避免阻塞主机启动。
    /// </summary>
    private void StartApiWarmupInBackground()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var warmupCts = new CancellationTokenSource(TimeSpan.FromSeconds(WarmupTimeoutSeconds));
                await WarmupReadPathAsync(warmupCts.Token);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("启动预热执行超时（>{TimeoutSeconds}s），已跳过剩余预热步骤。", WarmupTimeoutSeconds);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "启动预热执行失败，已降级跳过，不影响主机可用性。");
            }
        });
    }

    /// <summary>
    /// 预热高频查询读路径与关键仓储定位查询。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    private async Task WarmupReadPathAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var provider = scope.ServiceProvider;
        var now = DateTime.Now;
        var startTimeLocal = now.AddHours(-1);
        var endTimeLocal = now.AddHours(1);

        await TryWarmupStepAsync(
            "EF模型与分表上下文缓存",
            async () => await WarmupDbContextCacheAsync(provider, ct));
        await TryWarmupStepAsync(
            "总看板查询链路",
            async () => await provider.GetRequiredService<IGlobalDashboardQueryService>().QueryAsync(
                new GlobalDashboardQueryRequest
                {
                    StartTimeLocal = startTimeLocal,
                    EndTimeLocal = endTimeLocal
                },
                ct));
        await TryWarmupStepAsync(
            "码头看板查询链路",
            async () => await provider.GetRequiredService<IDockDashboardQueryService>().QueryAsync(
                new DockDashboardQueryRequest
                {
                    StartTimeLocal = startTimeLocal,
                    EndTimeLocal = endTimeLocal
                },
                ct));
        await TryWarmupStepAsync(
            "波次选项查询链路",
            async () => await provider.GetRequiredService<IWaveQueryService>().QueryOptionsAsync(
                new WaveOptionsQueryRequest
                {
                    StartTimeLocal = startTimeLocal,
                    EndTimeLocal = endTimeLocal
                },
                ct));
        await TryWarmupStepAsync(
            "波次摘要查询链路",
            async () => await provider.GetRequiredService<IWaveQueryService>().QuerySummaryAsync(
                new WaveSummaryQueryRequest
                {
                    StartTimeLocal = startTimeLocal,
                    EndTimeLocal = endTimeLocal,
                    WaveCode = WarmupWaveCode
                },
                ct));
        await TryWarmupStepAsync(
            "波次分区查询链路",
            async () => await provider.GetRequiredService<IWaveQueryService>().QueryZonesAsync(
                new WaveZoneQueryRequest
                {
                    StartTimeLocal = startTimeLocal,
                    EndTimeLocal = endTimeLocal,
                    WaveCode = WarmupWaveCode
                },
                ct));
        await TryWarmupStepAsync(
            "高频仓储定位查询链路",
            async () =>
            {
                var repository = provider.GetRequiredService<IBusinessTaskRepository>();
                await repository.FindByBarcodeAsync(WarmupBarcode, ct);
                await repository.FindByTaskCodeAsync(WarmupTaskCode, ct);
                await repository.FindBySourceTableAndBusinessKeyAsync(WarmupSourceTableCode, WarmupBusinessKey, ct);
            });
        logger.LogInformation("启动预热执行完成。");
    }

    /// <summary>
    /// 预热 EF Core 模型缓存与分表上下文初始化。
    /// </summary>
    /// <param name="serviceProvider">服务提供器。</param>
    /// <param name="ct">取消令牌。</param>
    private static async Task WarmupDbContextCacheAsync(IServiceProvider serviceProvider, CancellationToken ct)
    {
        var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<HubDbContext>>();
        var shardSuffixResolver = serviceProvider.GetRequiredService<IShardSuffixResolver>();

        await WarmupSingleDbContextAsync(dbContextFactory, string.Empty, ct);
        await WarmupSingleDbContextAsync(dbContextFactory, shardSuffixResolver.ResolveLocal(DateTime.Now), ct);
    }

    /// <summary>
    /// 预热单个分片后缀对应的 DbContext 模型与只读查询编译缓存。
    /// </summary>
    /// <param name="dbContextFactory">DbContext 工厂。</param>
    /// <param name="suffix">分片后缀。</param>
    /// <param name="ct">取消令牌。</param>
    private static async Task WarmupSingleDbContextAsync(
        IDbContextFactory<HubDbContext> dbContextFactory,
        string suffix,
        CancellationToken ct)
    {
        using var tableSuffixScope = TableSuffixScope.Use(suffix);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        _ = dbContext.Model;
        // 触发业务任务查询编译缓存与分表上下文模型热身。
        _ = await dbContext.BusinessTasks.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync(ct);
        // 触发扫描日志查询编译缓存。
        _ = await dbContext.ScanLogs.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync(ct);
        // 触发落格日志查询编译缓存。
        _ = await dbContext.DropLogs.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync(ct);
    }

    /// <summary>
    /// 执行单个预热步骤，失败时记录日志并继续后续步骤。
    /// </summary>
    /// <param name="stepName">步骤名称。</param>
    /// <param name="action">步骤动作。</param>
    private async Task TryWarmupStepAsync(string stepName, Func<Task> action)
    {
        try
        {
            await action();
            logger.LogInformation("启动预热步骤完成：{StepName}", stepName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "启动预热步骤失败，已跳过：{StepName}", stepName);
        }
    }

    /// <summary>
    /// 从异常链中提取第一个 <see cref="SqlException"/> 实例；若不存在则返回 <c>null</c>。
    /// </summary>
    /// <param name="exception">待遍历的异常对象。</param>
    /// <returns>找到的 <see cref="SqlException"/>，或 <c>null</c>。</returns>
    private static SqlException? TryGetSqlException(Exception exception) {
        var current = exception;
        while (current is not null) {
            if (current is SqlException sqlException) {
                return sqlException;
            }

            current = current.InnerException;
        }

        return null;
    }

    /// <summary>
    /// 判断异常链中是否包含数据库连接类异常。
    /// </summary>
    /// <param name="exception">待判断异常。</param>
    /// <returns>包含数据库连接类异常返回 <c>true</c>。</returns>
    private static bool IsDatabaseConnectivityException(Exception exception) {
        var current = exception;
        while (current is not null) {
            if (current is SqlException sqlException && IsSqlConnectivityException(sqlException)) {
                return true;
            }

            if (current is DbException dbException && IsDbConnectivityException(dbException)) {
                return true;
            }

            if (current is RetryLimitExceededException retryLimitExceededException
                && retryLimitExceededException.InnerException is not null
                && IsDatabaseConnectivityException(retryLimitExceededException.InnerException)) {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }

    /// <summary>
    /// 判断指定 <see cref="SqlException"/> 是否属于连接类异常。
    /// </summary>
    /// <param name="exception">SQL 异常对象。</param>
    /// <returns>连接类异常返回 <c>true</c>。</returns>
    private static bool IsSqlConnectivityException(SqlException exception) {
        if (exception.IsTransient) {
            return true;
        }

        foreach (SqlError error in exception.Errors) {
            switch (error.Number) {
                case -2:
                case 2:
                case 53:
                case 233:
                case 258:
                case 4060:
                case 10053:
                case 10054:
                case 10060:
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断指定 <see cref="DbException"/> 是否属于连接类异常。
    /// </summary>
    /// <param name="exception">数据库异常对象。</param>
    /// <returns>连接类异常返回 <c>true</c>。</returns>
    private static bool IsDbConnectivityException(DbException exception) {
        if (exception is SqlException sqlException) {
            return IsSqlConnectivityException(sqlException);
        }

        return exception.IsTransient;
    }

    /// <summary>
    /// 构建 SQL 错误码输出字符串。
    /// </summary>
    /// <param name="exception">SQL 异常对象。</param>
    /// <returns>错误码逗号分隔字符串。</returns>
    private static string BuildSqlErrorNumbers(SqlException exception) {
        if (exception.Errors.Count == 0) {
            return exception.Number.ToString();
        }

        return string.Join(",", exception.Errors.Cast<SqlError>().Select(error => error.Number.ToString()));
    }
}
