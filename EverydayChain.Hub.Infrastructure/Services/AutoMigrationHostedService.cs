using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Infrastructure.Services;

public class AutoMigrationHostedService(IServiceScopeFactory scopeFactory, ILogger<AutoMigrationHostedService> logger) : IHostedService {
    public async Task StartAsync(CancellationToken cancellationToken) {
        logger.LogInformation("启动自动迁移与分表自治流程。");
        using var scope = scopeFactory.CreateScope();
        var autoMigrationService = scope.ServiceProvider.GetRequiredService<IAutoMigrationService>();
        await autoMigrationService.RunAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
