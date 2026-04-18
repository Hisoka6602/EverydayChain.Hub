using EverydayChain.Hub.Host.Workers;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Host.Workers;

/// <summary>
/// AutoMigrationHostedService 启动容错行为测试。
/// </summary>
public sealed class AutoMigrationHostedServiceTests
{
    /// <summary>
    /// 自动迁移阶段抛出异常时应降级继续启动，不抛出到宿主。
    /// </summary>
    [Fact]
    public async Task StartAsync_ShouldNotThrow_WhenAutoMigrationStageThrows()
    {
        var migrationService = new TestAutoMigrationService
        {
            ExceptionToThrow = new TestDatabaseException("测试桩：数据库连接失败。")
        };
        var serviceProvider = BuildServiceProvider(migrationService);
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var runtimeStorageGuard = new TestRuntimeStorageGuard();
        var hostedService = new AutoMigrationHostedService(
            scopeFactory,
            runtimeStorageGuard,
            NullLogger<AutoMigrationHostedService>.Instance);

        await hostedService.StartAsync(CancellationToken.None);

        Assert.Equal(1, runtimeStorageGuard.StartupHealthCheckCount);
        Assert.Equal(1, migrationService.RunCount);
    }

    /// <summary>
    /// 启动自检阶段异常仍应中断启动，避免掩盖非数据库前置条件问题。
    /// </summary>
    [Fact]
    public async Task StartAsync_ShouldThrow_WhenStartupHealthCheckThrows()
    {
        var migrationService = new TestAutoMigrationService();
        var serviceProvider = BuildServiceProvider(migrationService);
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var runtimeStorageGuard = new TestRuntimeStorageGuard
        {
            StartupExceptionToThrow = new InvalidOperationException("测试桩：启动自检失败。")
        };
        var hostedService = new AutoMigrationHostedService(
            scopeFactory,
            runtimeStorageGuard,
            NullLogger<AutoMigrationHostedService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => hostedService.StartAsync(CancellationToken.None));
        Assert.Equal(1, runtimeStorageGuard.StartupHealthCheckCount);
        Assert.Equal(0, migrationService.RunCount);
    }

    /// <summary>
    /// 构建测试用服务提供器。
    /// </summary>
    /// <param name="migrationService">自动迁移服务桩。</param>
    /// <returns>服务提供器。</returns>
    private static ServiceProvider BuildServiceProvider(TestAutoMigrationService migrationService)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => migrationService);
        services.AddScoped<IAutoMigrationService>(sp => sp.GetRequiredService<TestAutoMigrationService>());
        return services.BuildServiceProvider();
    }
}
