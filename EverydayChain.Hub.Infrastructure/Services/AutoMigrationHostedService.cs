using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 托管服务入口，在应用启动阶段触发自动迁移与分表预置流程。
/// </summary>
public class AutoMigrationHostedService(
    IServiceScopeFactory scopeFactory,
    IRuntimeStorageGuard runtimeStorageGuard,
    ILogger<AutoMigrationHostedService> logger) : IHostedService
{
    /// <summary>
    /// 应用启动时调用，创建作用域并执行 <see cref="IAutoMigrationService.RunAsync"/>。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var currentStage = "启动初始化阶段";
        try
        {
            logger.LogInformation("启动自动迁移与分表自治流程。");
            currentStage = "启动自检阶段";
            await runtimeStorageGuard.EnsureStartupHealthyAsync(cancellationToken);
            using var scope = scopeFactory.CreateScope();
            var autoMigrationService = scope.ServiceProvider.GetRequiredService<IAutoMigrationService>();
            currentStage = "自动迁移阶段";
            await autoMigrationService.RunAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "自动迁移与分表自治流程在{Stage}发生异常，应用启动终止。", currentStage);
            throw;
        }
    }

    /// <summary>
    /// 应用停止时调用，无需执行任何清理。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
