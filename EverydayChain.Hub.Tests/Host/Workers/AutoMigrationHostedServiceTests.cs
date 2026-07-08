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
        Assert.Equal(1, databaseConnectivityService.GetSnapshotCount);
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
        Assert.Equal(0, databaseConnectivityService.GetSnapshotCount);
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
        Assert.Equal(1, databaseConnectivityService.GetSnapshotCount);
        Assert.Equal(0, databaseConnectivityService.RefreshSnapshotCount);
    }

    [Fact]
    public async Task StartAsync_ShouldRefreshConnectivitySnapshot_WhenAutoMigrationSucceeds()
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
        Assert.Equal(1, databaseConnectivityService.GetSnapshotCount);
        Assert.Equal(1, databaseConnectivityService.RefreshSnapshotCount);
    }

    [Fact]
    public async Task StartAsync_ShouldAttemptAutoMigration_WhenLocalSqlServerIsUnavailable()
    {
        var migrationService = new TestAutoMigrationService();
        var databaseConnectivityService = new TestDatabaseConnectivityService
        {
            Snapshot = new DatabaseConnectivitySnapshot
            {
                CheckedAtLocal = DateTime.Now,
                LocalSqlServer = new DatabaseEndpointConnectivityState
                {
                    DatabaseName = "本地 MSSQL",
                    IsAvailable = false,
                    Description = "本地 MSSQL 无法连接（测试桩）。"
                },
                Oracle = new DatabaseEndpointConnectivityState
                {
                    DatabaseName = "远端 Oracle",
                    IsAvailable = true,
                    Description = "远端 Oracle 连接正常"
                }
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
        Assert.Equal(1, databaseConnectivityService.GetSnapshotCount);
        Assert.Equal(1, databaseConnectivityService.RefreshSnapshotCount);
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
        /// 预置快照。
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
        /// 快照读取次数。
        /// </summary>
        public int GetSnapshotCount { get; private set; }

        /// <summary>
        /// 快照刷新次数。
        /// </summary>
        public int RefreshSnapshotCount { get; private set; }

        /// <summary>
        /// 获取数据库连通性快照。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>数据库连通性快照。</returns>
        public Task<DatabaseConnectivitySnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            GetSnapshotCount++;
            return Task.FromResult(Snapshot);
        }

        /// <summary>
        /// 强制刷新数据库连通性快照。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>数据库连通性快照。</returns>
        public Task<DatabaseConnectivitySnapshot> RefreshSnapshotAsync(CancellationToken cancellationToken)
        {
            RefreshSnapshotCount++;
            return Task.FromResult(Snapshot);
        }

        /// <summary>
        /// 判断异常是否属于数据库连接类异常。
        /// </summary>
        /// <param name="exception">待识别异常。</param>
        /// <returns>是则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
        public bool IsDatabaseConnectivityException(Exception exception)
        {
            return exception is TestDatabaseException;
        }
    }
}
