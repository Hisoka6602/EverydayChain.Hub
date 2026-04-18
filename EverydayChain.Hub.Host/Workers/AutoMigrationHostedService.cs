using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
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

    /// <summary>
    /// 应用启动时调用，创建作用域并执行 <see cref="IAutoMigrationService.RunAsync"/>。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task StartAsync(CancellationToken cancellationToken) {
        var currentStage = "启动初始化阶段";
        try {
            logger.LogInformation("启动自动迁移与分表自治流程。");
            currentStage = "启动自检阶段";
            using var startupTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            startupTimeoutCts.CancelAfter(TimeSpan.FromSeconds(StartupStageTimeoutSeconds));
            var startupToken = startupTimeoutCts.Token;
            await runtimeStorageGuard.EnsureStartupHealthyAsync(startupToken);
            using var scope = scopeFactory.CreateScope();
            var autoMigrationService = scope.ServiceProvider.GetRequiredService<IAutoMigrationService>();
            currentStage = AutoMigrationStage;
            await autoMigrationService.RunAsync(startupToken);
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
