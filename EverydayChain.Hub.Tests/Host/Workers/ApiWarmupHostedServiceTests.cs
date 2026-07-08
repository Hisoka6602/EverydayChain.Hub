using EverydayChain.Hub.Application.Abstractions.Infrastructure;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Host.Workers;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Tests.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Host.Workers;

public sealed class ApiWarmupHostedServiceTests
{
    [Fact]
    public async Task StartAsync_ShouldNotThrow_WhenWarmupServiceThrows()
    {
        var hostedService = CreateHostedService(new ThrowingApiWarmupService(), new StubDatabaseConnectivityService());

        await hostedService.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_ShouldInvokeWarmupService()
    {
        var warmupService = new RecordingApiWarmupService();
        var hostedService = CreateHostedService(warmupService, new StubDatabaseConnectivityService());

        await hostedService.StartAsync(CancellationToken.None);
        var isCompleted = await warmupService.InvocationCompletion.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.True(isCompleted);
        Assert.Equal(1, warmupService.InvocationCount);
    }

    [Fact]
    public async Task StartAsync_ShouldSkipWarmup_WhenLocalSqlServerIsUnavailable()
    {
        var warmupService = new RecordingApiWarmupService();
        var connectivityService = new StubDatabaseConnectivityService
        {
            Snapshot = CreateLocalSqlUnavailableSnapshot()
        };
        var hostedService = CreateHostedService(warmupService, connectivityService);

        await hostedService.StartAsync(CancellationToken.None);
        await Task.Delay(200);

        Assert.Equal(0, warmupService.InvocationCount);
    }

    [Fact]
    public async Task StartAsync_ShouldInvokeWarmupService_WhenOnlyOracleIsUnavailable()
    {
        var warmupService = new RecordingApiWarmupService();
        var connectivityService = new StubDatabaseConnectivityService
        {
            Snapshot = CreateOracleUnavailableSnapshot()
        };
        var hostedService = CreateHostedService(warmupService, connectivityService);

        await hostedService.StartAsync(CancellationToken.None);
        var isCompleted = await warmupService.InvocationCompletion.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.True(isCompleted);
        Assert.Equal(1, warmupService.InvocationCount);
    }

    private static ApiWarmupHostedService CreateHostedService(
        IApiWarmupService apiWarmupService,
        IDatabaseConnectivityService databaseConnectivityService)
    {
        var dbContextFactory = new ThrowingHubDbContextFactory();
        IShardSuffixResolver shardSuffixResolver = new FixedBootstrapShardSuffixResolver(["_202604"]);

        return new ApiWarmupHostedService(
            apiWarmupService,
            databaseConnectivityService,
            dbContextFactory,
            shardSuffixResolver,
            new TestHostApplicationLifetime(),
            NullLogger<ApiWarmupHostedService>.Instance);
    }

    private static DatabaseConnectivitySnapshot CreateLocalSqlUnavailableSnapshot()
    {
        return new DatabaseConnectivitySnapshot
        {
            CheckedAtLocal = DateTime.Now,
            LocalSqlServer = new DatabaseEndpointConnectivityState
            {
                DatabaseName = "本地 MSSQL",
                IsAvailable = false,
                Description = "本地 MSSQL 无法连接（连接超时）"
            },
            Oracle = new DatabaseEndpointConnectivityState
            {
                DatabaseName = "远端 Oracle",
                IsAvailable = true,
                Description = "远端 Oracle 连接正常"
            }
        };
    }

    private static DatabaseConnectivitySnapshot CreateOracleUnavailableSnapshot()
    {
        return new DatabaseConnectivitySnapshot
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
                IsAvailable = false,
                Description = "远端 Oracle 无法连接（监听未注册）"
            }
        };
    }

    private sealed class RecordingApiWarmupService : IApiWarmupService
    {
        /// <summary>
        /// 获取预热调用次数。
        /// </summary>
        public int InvocationCount { get; private set; }

        /// <summary>
        /// 获取预热调用完成通知。
        /// </summary>
        public TaskCompletionSource<bool> InvocationCompletion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task WarmupAsync(CancellationToken cancellationToken)
        {
            InvocationCount++;
            InvocationCompletion.TrySetResult(true);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingApiWarmupService : IApiWarmupService
    {
        public Task WarmupAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("测试桩：预热失败。");
        }
    }

    private sealed class TestHostApplicationLifetime : IHostApplicationLifetime
    {
        /// <summary>
        /// 获取应用已启动令牌。
        /// </summary>
        public CancellationToken ApplicationStarted => CancellationToken.None;

        /// <summary>
        /// 获取应用停止中令牌。
        /// </summary>
        public CancellationToken ApplicationStopping => CancellationToken.None;

        /// <summary>
        /// 获取应用已停止令牌。
        /// </summary>
        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication()
        {
        }
    }

    private sealed class StubDatabaseConnectivityService : IDatabaseConnectivityService
    {
        /// <summary>
        /// 获取或设置数据库连通性快照。
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

        public Task<DatabaseConnectivitySnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Snapshot);
        }

        public Task<DatabaseConnectivitySnapshot> RefreshSnapshotAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Snapshot);
        }

        public bool IsDatabaseConnectivityException(Exception exception)
        {
            return false;
        }
    }
}
