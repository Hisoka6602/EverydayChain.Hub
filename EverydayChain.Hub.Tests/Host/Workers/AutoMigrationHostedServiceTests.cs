using EverydayChain.Hub.Host.Workers;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Host.Workers;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class AutoMigrationHostedServiceTests
{
    [Fact]
    public async Task StartAsync_ShouldNotThrow_WhenAutoMigrationStageThrows()
    {
        var migrationService = new TestAutoMigrationService
        {
            ExceptionToThrow = new TestDatabaseException("测试桩：数据库连接失败。", isTransient: true)
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

    [Fact]
    public async Task StartAsync_ShouldThrow_WhenAutoMigrationStageThrowsNonDatabaseException()
    {
        var migrationService = new TestAutoMigrationService
        {
            ExceptionToThrow = new InvalidOperationException("测试桩：自动迁移配置错误。")
        };
        var serviceProvider = BuildServiceProvider(migrationService);
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var runtimeStorageGuard = new TestRuntimeStorageGuard();
        var hostedService = new AutoMigrationHostedService(
            scopeFactory,
            runtimeStorageGuard,
            NullLogger<AutoMigrationHostedService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => hostedService.StartAsync(CancellationToken.None));
        Assert.Equal(1, runtimeStorageGuard.StartupHealthCheckCount);
        Assert.Equal(1, migrationService.RunCount);
    }

    [Fact]
    public async Task StartAsync_ShouldNotThrow_WhenWarmupDependenciesMissing()
    {
        var migrationService = new TestAutoMigrationService();
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

    private static ServiceProvider BuildServiceProvider(TestAutoMigrationService migrationService)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => migrationService);
        services.AddScoped<IAutoMigrationService>(sp => sp.GetRequiredService<TestAutoMigrationService>());
        return services.BuildServiceProvider();
    }
}

