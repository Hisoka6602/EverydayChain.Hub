using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using EverydayChain.Hub.Infrastructure.Services;

namespace EverydayChain.Hub.Host.Workers;

/// <summary>
/// 定义当前类型。
/// </summary>
public class AutoMigrationHostedService(
    IServiceScopeFactory scopeFactory,
    IRuntimeStorageGuard runtimeStorageGuard,
    ILogger<AutoMigrationHostedService> logger) : IHostedService {

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string AutoMigrationStage = "自动迁移阶段";
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const int StartupStageTimeoutSeconds = 120;
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken) {
        // 步骤：按既定流程执行当前方法逻辑。
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
    /// 执行当前方法。
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static SqlException? TryGetSqlException(Exception exception) {
        // 步骤：按既定流程执行当前方法逻辑。
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
    /// 执行当前方法。
    /// </summary>
    private static bool IsDatabaseConnectivityException(Exception exception) {
        // 步骤：按既定流程执行当前方法逻辑。
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
    /// 执行当前方法。
    /// </summary>
    private static bool IsSqlConnectivityException(SqlException exception) {
        // 步骤：按既定流程执行当前方法逻辑。
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
    /// 执行当前方法。
    /// </summary>
    private static bool IsDbConnectivityException(DbException exception) {
        // 步骤：按既定流程执行当前方法逻辑。
        if (exception is SqlException sqlException) {
            return IsSqlConnectivityException(sqlException);
        }

        return exception.IsTransient;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static string BuildSqlErrorNumbers(SqlException exception) {
        // 步骤：按既定流程执行当前方法逻辑。
        if (exception.Errors.Count == 0) {
            return exception.Number.ToString();
        }

        return string.Join(",", exception.Errors.Cast<SqlError>().Select(error => error.Number.ToString()));
    }
}


