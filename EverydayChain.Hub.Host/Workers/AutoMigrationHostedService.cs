using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.Storage;
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

    /// <summary>
    /// 应用启动时调用，创建作用域并执行 <see cref="IAutoMigrationService.RunAsync"/>。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task StartAsync(CancellationToken cancellationToken) {
        var currentStage = "启动初始化阶段";
        try {
            logger.LogInformation("启动自动迁移与分表自治流程。");
            currentStage = "启动自检阶段";
            await runtimeStorageGuard.EnsureStartupHealthyAsync(cancellationToken);
            using var scope = scopeFactory.CreateScope();
            var autoMigrationService = scope.ServiceProvider.GetRequiredService<IAutoMigrationService>();
            currentStage = AutoMigrationStage;
            await autoMigrationService.RunAsync(cancellationToken);
        }
        catch (Exception ex) when (string.Equals(currentStage, AutoMigrationStage, StringComparison.Ordinal)) {
            if (TryGetSqlException(ex) is { Number: 10054 } sqlException) {
                logger.LogError(
                    ex,
                    "自动迁移阶段数据库握手失败（SqlError={SqlError}）。常见原因：1) SQL Server TLS 版本与客户端不兼容；2) Encrypt/TrustServerCertificate 配置不匹配；3) 网络设备或防火墙中断连接；4) SQL Server 负载过高或重启中。已降级跳过自动迁移并继续启动。ClientConnectionId={ClientConnectionId}",
                    sqlException.Number,
                    sqlException.ClientConnectionId);
            }
            else {
                logger.LogError(ex, "自动迁移阶段发生异常，已降级跳过自动迁移并继续启动。");
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
}
