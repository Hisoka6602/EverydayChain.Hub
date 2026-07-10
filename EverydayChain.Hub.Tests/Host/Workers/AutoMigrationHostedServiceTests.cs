using EverydayChain.Hub.Application.Abstractions.Infrastructure;
using EverydayChain.Hub.Host.Workers;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Host.Workers;

/// <summary>
/// 自动迁移启动托管服务测试。
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
        var databaseConnectivityService = new TestDatabaseConnectivityService();
        var serviceProvider = BuildServiceProvider(migrationService);
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var runtimeStorageGuard = new TestRuntimeStorageGuard();
        var hostedService = new AutoMigrationHostedService(
            scopeFactory,
            runtimeStorageGuard,
            databaseConnectivityService,
            NullLogger<AutoMigrationHostedService>.Instance);

        await hostedService.StartAsync(CancellationToken.None);

        Assert.Equal(1, runtimeStorageGuard.StartupHealthCheckCount);
        Assert.Equal(1, migrationService.RunCount);
        Assert.Equal(1, databaseConnectivityService.GetLocalSqlServerStateCount);
        Assert.Equal(0, databaseConnectivityService.GetSnapshotCount);
        Assert.Equal(0, databaseConnectivityService.RefreshLocalSqlServerStateCount);
        Assert.Equal(0, databaseConnectivityService.RefreshSnapshotCount);
    }

    [Fact]
    public async Task StartAsync_ShouldThrow_WhenStartupHealthCheckThrows()
    {
        var migrationService = new TestAutoMigrationService();
        var databaseConnectivityService = new TestDatabaseConnectivityService();
        var serviceProvider = BuildServiceProvider(migrationService);
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var runtimeStorageGuard = new TestRuntimeStorageGuard
        {
            StartupExceptionToThrow = new InvalidOperationException("测试桩：启动自检失败。")
        };
        var hostedService = new AutoMigrationHostedService(
            scopeFactory,
            runtimeStorageGuard,
            databaseConnectivityService,
            NullLogger<AutoMigrationHostedService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => hostedService.StartAsync(CancellationToken.None));
        Assert.Equal(1, runtimeStorageGuard.StartupHealthCheckCount);
        Assert.Equal(0, migrationService.RunCount);
        Assert.Equal(0, databaseConnectivityService.GetLocalSqlServerStateCount);
        Assert.Equal(0, databaseConnectivityService.GetSnapshotCount);
        Assert.Equal(0, databaseConnectivityService.RefreshLocalSqlServerStateCount);
        Assert.Equal(0, databaseConnectivityService.RefreshSnapshotCount);
    }

    [Fact]
    public async Task StartAsync_ShouldThrow_WhenAutoMigrationStageThrowsNonDatabaseException()
    {
        var migrationService = new TestAutoMigrationService
        {
            ExceptionToThrow = new InvalidOperationException("测试桩：自动迁移配置错误。")
        };
        var databaseConnectivityService = new TestDatabaseConnectivityService();
        var serviceProvider = BuildServiceProvider(migrationService);
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var runtimeStorageGuard = new TestRuntimeStorageGuard();
        var hostedService = new AutoMigrationHostedService(
            scopeFactory,
            runtimeStorageGuard,
            databaseConnectivityService,
            NullLogger<AutoMigrationHostedService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => hostedService.StartAsync(CancellationToken.None));
        Assert.Equal(1, runtimeStorageGuard.StartupHealthCheckCount);
        Assert.Equal(1, migrationService.RunCount);
        Assert.Equal(1, databaseConnectivityService.GetLocalSqlServerStateCount);
        Assert.Equal(0, databaseConnectivityService.GetSnapshotCount);
        Assert.Equal(0, databaseConnectivityService.RefreshLocalSqlServerStateCount);
        Assert.Equal(0, databaseConnectivityService.RefreshSnapshotCount);
    }

    [Fact]
    public async Task StartAsync_ShouldRefreshLocalSqlServerState_WhenAutoMigrationSucceeds()
    {
        var migrationService = new TestAutoMigrationService();
        var databaseConnectivityService = new TestDatabaseConnectivityService();
        var serviceProvider = BuildServiceProvider(migrationService);
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var runtimeStorageGuard = new TestRuntimeStorageGuard();
        var hostedService = new AutoMigrationHostedService(
            scopeFactory,
            runtimeStorageGuard,
            databaseConnectivityService,
            NullLogger<AutoMigrationHostedService>.Instance);

        await hostedService.StartAsync(CancellationToken.None);

        Assert.Equal(1, runtimeStorageGuard.StartupHealthCheckCount);
        Assert.Equal(1, migrationService.RunCount);
        Assert.Equal(1, databaseConnectivityService.GetLocalSqlServerStateCount);
        Assert.Equal(0, databaseConnectivityService.GetSnapshotCount);
        Assert.Equal(1, databaseConnectivityService.RefreshLocalSqlServerStateCount);
        Assert.Equal(0, databaseConnectivityService.RefreshSnapshotCount);
    }

    [Fact]
    public async Task StartAsync_ShouldAttemptAutoMigration_WhenLocalSqlServerIsUnavailable()
    {
        var migrationService = new TestAutoMigrationService();
        var databaseConnectivityService = new TestDatabaseConnectivityService
        {
            LocalSqlServerState = new DatabaseEndpointConnectivityState
            {
                DatabaseName = "本地 MSSQL",
                IsAvailable = false,
                Description = "本地 MSSQL 无法连接（测试桩）"
            }
        };
        var serviceProvider = BuildServiceProvider(migrationService);
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var runtimeStorageGuard = new TestRuntimeStorageGuard();
        var hostedService = new AutoMigrationHostedService(
            scopeFactory,
            runtimeStorageGuard,
            databaseConnectivityService,
            NullLogger<AutoMigrationHostedService>.Instance);

        await hostedService.StartAsync(CancellationToken.None);

        Assert.Equal(1, runtimeStorageGuard.StartupHealthCheckCount);
        Assert.Equal(1, migrationService.RunCount);
        Assert.Equal(1, databaseConnectivityService.GetLocalSqlServerStateCount);
        Assert.Equal(0, databaseConnectivityService.GetSnapshotCount);
        Assert.Equal(1, databaseConnectivityService.RefreshLocalSqlServerStateCount);
        Assert.Equal(0, databaseConnectivityService.RefreshSnapshotCount);
    }

    /// <summary>
    /// 构建测试用依赖注入容器。
    /// </summary>
    /// <param name="migrationService">自动迁移测试桩。</param>
    /// <returns>服务提供器。</returns>
    private static ServiceProvider BuildServiceProvider(TestAutoMigrationService migrationService)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => migrationService);
        services.AddScoped<IAutoMigrationService>(sp => sp.GetRequiredService<TestAutoMigrationService>());
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// 数据库连通性测试桩。
    /// </summary>
    private sealed class TestDatabaseConnectivityService : IDatabaseConnectivityService
    {
        /// <summary>
        /// 预置快速本地 MSSQL 状态。
        /// </summary>
        public DatabaseEndpointConnectivityState LocalSqlServerState { get; set; } = new()
        {
            DatabaseName = "本地 MSSQL",
            IsAvailable = true,
            Description = "本地 MSSQL 连接正常"
        };

        /// <summary>
        /// 预置完整快照。
        /// </summary>
        public DatabaseConnectivitySnapshot Snapshot { get; set; } = new()
        {
            CheckedAtLocal = DateTime.Now,
            LocalSqlServer = new DatabaseEndpointConnectivityState
            {
                DatabaseName = "本地 MSSQL",
                IsAvailable = true,
                Description = "本地 MSSQL 连接正常"
            },
            Oracle = new DatabaseEndpointConnectivityState
            {
                DatabaseName = "远端 Oracle",
                IsAvailable = true,
                Description = "远端 Oracle 连接正常"
            }
        };

        /// <summary>
        /// 本地 MSSQL 状态读取次数。
        /// </summary>
        public int GetLocalSqlServerStateCount { get; private set; }

        /// <summary>
        /// 完整快照读取次数。
        /// </summary>
        public int GetSnapshotCount { get; private set; }

        /// <summary>
        /// 本地 MSSQL 状态刷新次数。
        /// </summary>
        public int RefreshLocalSqlServerStateCount { get; private set; }

        /// <summary>
        /// 完整快照刷新次数。
        /// </summary>
        public int RefreshSnapshotCount { get; private set; }

        public Task<DatabaseConnectivitySnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            GetSnapshotCount++;
            return Task.FromResult(Snapshot);
        }

        public Task<DatabaseConnectivitySnapshot> RefreshSnapshotAsync(CancellationToken cancellationToken)
        {
            RefreshSnapshotCount++;
            return Task.FromResult(Snapshot);
        }

        public Task<DatabaseEndpointConnectivityState> GetLocalSqlServerStateAsync(CancellationToken cancellationToken)
        {
            GetLocalSqlServerStateCount++;
            return Task.FromResult(LocalSqlServerState);
        }

        public Task<DatabaseEndpointConnectivityState> RefreshLocalSqlServerStateAsync(CancellationToken cancellationToken)
        {
            RefreshLocalSqlServerStateCount++;
            return Task.FromResult(LocalSqlServerState);
        }

        public bool IsDatabaseConnectivityException(Exception exception)
        {
            return exception is TestDatabaseException;
        }
    }
}
