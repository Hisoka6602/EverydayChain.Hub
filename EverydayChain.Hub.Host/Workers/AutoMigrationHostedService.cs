using EverydayChain.Hub.Application.Abstractions.Infrastructure;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Host.Workers;

/// <summary>
/// 启动期执行自动迁移与分表自治流程的托管服务。
/// </summary>
public class AutoMigrationHostedService(
    IServiceScopeFactory scopeFactory,
    IRuntimeStorageGuard runtimeStorageGuard,
    IDatabaseConnectivityService databaseConnectivityService,
    ILogger<AutoMigrationHostedService> logger) : IHostedService
{
    /// <summary>
    /// 自动迁移阶段名称。
    /// </summary>
    private const string AutoMigrationStage = "自动迁移阶段";

    /// <summary>
    /// 启动阶段超时秒数。
    /// </summary>
    private const int StartupStageTimeoutSeconds = 120;

    /// <summary>
    /// 执行启动阶段逻辑。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var currentStage = "启动初始化阶段";
        try
        {
            logger.LogInformation("启动自动迁移与分表自治流程。");

            currentStage = "启动自检阶段";
            using var healthCheckCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            healthCheckCts.CancelAfter(TimeSpan.FromSeconds(StartupStageTimeoutSeconds));
            await runtimeStorageGuard.EnsureStartupHealthyAsync(healthCheckCts.Token);

            currentStage = "数据库可达性预检阶段";
            using var connectivityCheckCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connectivityCheckCts.CancelAfter(TimeSpan.FromSeconds(StartupStageTimeoutSeconds));
            var connectivitySnapshot = await databaseConnectivityService.GetSnapshotAsync(connectivityCheckCts.Token);
            if (!connectivitySnapshot.LocalSqlServer.IsAvailable)
            {
                logger.LogWarning(
                    "本地 MSSQL 连通性预检未通过：{Description}。将继续尝试自动迁移，以覆盖目标数据库缺失等可自愈场景。",
                    connectivitySnapshot.LocalSqlServer.Description);
            }

            using var scope = scopeFactory.CreateScope();
            var autoMigrationService = scope.ServiceProvider.GetRequiredService<IAutoMigrationService>();
            currentStage = AutoMigrationStage;
            using var migrationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            migrationCts.CancelAfter(TimeSpan.FromSeconds(StartupStageTimeoutSeconds));
            await autoMigrationService.RunAsync(migrationCts.Token);

            currentStage = "数据库连通性刷新阶段";
            using var connectivityRefreshCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connectivityRefreshCts.CancelAfter(TimeSpan.FromSeconds(StartupStageTimeoutSeconds));
            await databaseConnectivityService.RefreshSnapshotAsync(connectivityRefreshCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(
                "自动迁移与分表自治流程在{Stage}执行超时（{TimeoutSeconds}s），已降级跳过并继续启动。",
                currentStage,
                StartupStageTimeoutSeconds);
        }
        catch (Exception ex) when (string.Equals(currentStage, AutoMigrationStage, StringComparison.Ordinal)
            && databaseConnectivityService.IsDatabaseConnectivityException(ex))
        {
            if (TryGetSqlException(ex) is SqlException sqlException)
            {
                logger.LogError(
                    ex,
                    "自动迁移阶段命中数据库连接类异常并已降级跳过。ConnectivityDegraded=true, SqlErrors={SqlErrors}, ClientConnectionId={ClientConnectionId}。",
                    BuildSqlErrorNumbers(sqlException),
                    sqlException.ClientConnectionId);
            }
            else
            {
                logger.LogError(
                    ex,
                    "自动迁移阶段命中数据库连接类异常并已降级跳过。ConnectivityDegraded=true。");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "自动迁移与分表自治流程在{Stage}发生异常，应用启动终止。", currentStage);
            throw;
        }
    }

    /// <summary>
    /// 执行停止阶段逻辑。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 从异常链中提取首个 SQL 异常。
    /// </summary>
    /// <param name="exception">异常对象。</param>
    /// <returns>找到时返回 SQL 异常，否则返回 <c>null</c>。</returns>
    private static SqlException? TryGetSqlException(Exception exception)
    {
        var current = exception;
        while (current is not null)
        {
            if (current is SqlException sqlException)
            {
                return sqlException;
            }

            current = current.InnerException;
        }

        return null;
    }

    /// <summary>
    /// 拼接 SQL 错误码列表。
    /// </summary>
    /// <param name="exception">SQL 异常对象。</param>
    /// <returns>逗号分隔的错误码。</returns>
    private static string BuildSqlErrorNumbers(SqlException exception)
    {
        if (exception.Errors.Count == 0)
        {
            return exception.Number.ToString();
        }

        return string.Join(",", exception.Errors.Cast<SqlError>().Select(error => error.Number.ToString()));
    }
}
