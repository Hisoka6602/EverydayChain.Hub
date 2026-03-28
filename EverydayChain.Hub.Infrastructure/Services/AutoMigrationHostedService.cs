using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 托管服务入口，在应用启动阶段触发自动迁移与分表预置流程。
/// </summary>
public class AutoMigrationHostedService(IServiceScopeFactory scopeFactory, ILogger<AutoMigrationHostedService> logger) : IHostedService
{
    /// <summary>
    /// 应用启动时调用，创建作用域并执行 <see cref="IAutoMigrationService.RunAsync"/>。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("启动自动迁移与分表自治流程。");
        using var scope = scopeFactory.CreateScope();
        var autoMigrationService = scope.ServiceProvider.GetRequiredService<IAutoMigrationService>();
        await autoMigrationService.RunAsync(cancellationToken);
    }

    /// <summary>
    /// 应用停止时调用，无需执行任何清理。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
