using EverydayChain.Hub.Application.Abstractions.Infrastructure;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Host.Startup;
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
        var hostedService = CreateHostedService(
            new ThrowingApiWarmupService(),
            new RecordingApiEndpointWarmupService(),
            new RecordingDashboardSnapshotService(),
            new StubDatabaseConnectivityService());

        await hostedService.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_ShouldInvokeWarmupServices()
    {
        var warmupService = new RecordingApiWarmupService();
        var endpointWarmupService = new RecordingApiEndpointWarmupService();
        var dashboardSnapshotService = new RecordingDashboardSnapshotService();
        var connectivityService = new StubDatabaseConnectivityService();
        var hostedService = CreateHostedService(warmupService, endpointWarmupService, dashboardSnapshotService, connectivityService);

        await hostedService.StartAsync(CancellationToken.None);
        await warmupService.InvocationCompletion.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await endpointWarmupService.InvocationCompletion.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(1, warmupService.InvocationCount);
        Assert.Equal(1, endpointWarmupService.InvocationCount);
        Assert.Equal(1, dashboardSnapshotService.InvocationCount);
        Assert.Equal(1, connectivityService.GetLocalSqlServerStateCount);
        Assert.Equal(0, connectivityService.GetSnapshotCount);
    }

    [Fact]
    public async Task StartAsync_ShouldSkipWarmup_WhenLocalSqlServerIsUnavailable()
    {
        var warmupService = new RecordingApiWarmupService();
        var endpointWarmupService = new RecordingApiEndpointWarmupService();
        var dashboardSnapshotService = new RecordingDashboardSnapshotService();
        var connectivityService = new StubDatabaseConnectivityService
        {
            LocalSqlServerState = CreateUnavailableLocalSqlServerState()
        };
        var hostedService = CreateHostedService(warmupService, endpointWarmupService, dashboardSnapshotService, connectivityService);

        await hostedService.StartAsync(CancellationToken.None);
        await Task.Delay(200);

        Assert.Equal(0, warmupService.InvocationCount);
        Assert.Equal(0, endpointWarmupService.InvocationCount);
        Assert.Equal(0, dashboardSnapshotService.InvocationCount);
        Assert.Equal(1, connectivityService.GetLocalSqlServerStateCount);
        Assert.Equal(0, connectivityService.GetSnapshotCount);
    }

    [Fact]
    public async Task StartAsync_ShouldUpdateWarmupState_WhenWarmupCompletes()
    {
        var warmupState = new ApiWarmupState();
        var warmupService = new RecordingApiWarmupService();
        var endpointWarmupService = new RecordingApiEndpointWarmupService();
        var dashboardSnapshotService = new RecordingDashboardSnapshotService();
        var hostedService = CreateHostedService(
            warmupService,
            endpointWarmupService,
            dashboardSnapshotService,
            new StubDatabaseConnectivityService(),
            warmupState);

        await hostedService.StartAsync(CancellationToken.None);
        await endpointWarmupService.InvocationCompletion.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var snapshot = warmupState.GetSnapshot();
        Assert.True(snapshot.HasStarted);
        Assert.False(snapshot.IsRunning);
        Assert.True(snapshot.IsCompleted);
        Assert.Equal("Completed", snapshot.Stage);
    }

    [Fact]
    public async Task StartAsync_ShouldKeepWarmQueries_WhenBackgroundWarmupIsEnabled()
    {
        var warmupService = new RecordingApiWarmupService();
        var endpointWarmupService = new RecordingApiEndpointWarmupService();
        var dashboardSnapshotService = new RecordingDashboardSnapshotService();
        var hostedService = CreateHostedService(
            warmupService,
            endpointWarmupService,
            dashboardSnapshotService,
            new StubDatabaseConnectivityService(),
            queryCacheOptions: new QueryCacheOptions
            {
                BackgroundWarmupEnabled = true,
                BackgroundWarmupIntervalSeconds = 1
            });

        await hostedService.StartAsync(CancellationToken.None);
        var deadline = DateTime.Now.AddSeconds(5);
        while (warmupService.InvocationCount < 2 && DateTime.Now < deadline)
        {
            await Task.Delay(200);
        }

        Assert.True(warmupService.InvocationCount >= 2);
        Assert.Equal(1, endpointWarmupService.InvocationCount);

        await hostedService.StopAsync(CancellationToken.None);
    }

    private static ApiWarmupHostedService CreateHostedService(
        IApiWarmupService apiWarmupService,
        IApiEndpointWarmupService apiEndpointWarmupService,
        IDashboardSnapshotService dashboardSnapshotService,
        IDatabaseConnectivityService databaseConnectivityService,
        IApiWarmupState? apiWarmupState = null,
        QueryCacheOptions? queryCacheOptions = null)
    {
        var dbContextFactory = new ThrowingHubDbContextFactory();
        IShardSuffixResolver shardSuffixResolver = new FixedBootstrapShardSuffixResolver(["_202604"]);

        return new ApiWarmupHostedService(
            apiWarmupService,
            apiEndpointWarmupService,
            dashboardSnapshotService,
            databaseConnectivityService,
            dbContextFactory,
            shardSuffixResolver,
            queryCacheOptions ?? new QueryCacheOptions
            {
                BackgroundWarmupEnabled = false
            },
            apiWarmupState ?? new ApiWarmupState(),
            new TestHostApplicationLifetime(),
            NullLogger<ApiWarmupHostedService>.Instance);
    }

    private static DatabaseEndpointConnectivityState CreateAvailableLocalSqlServerState()
    {
        return new DatabaseEndpointConnectivityState
        {
            DatabaseName = "LocalSqlServer",
            IsAvailable = true,
            Description = "LocalSqlServer available"
        };
    }

    private static DatabaseEndpointConnectivityState CreateUnavailableLocalSqlServerState()
    {
        return new DatabaseEndpointConnectivityState
        {
            DatabaseName = "LocalSqlServer",
            IsAvailable = false,
            Description = "LocalSqlServer unavailable"
        };
    }

    private sealed class RecordingApiWarmupService : IApiWarmupService
    {
        /// <summary>
        /// 获取或设置预热调用次数。
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

    private sealed class RecordingApiEndpointWarmupService : IApiEndpointWarmupService
    {
        /// <summary>
        /// 获取或设置 HTTP 端点预热调用次数。
        /// </summary>
        public int InvocationCount { get; private set; }

        /// <summary>
        /// 获取 HTTP 端点预热完成通知。
        /// </summary>
        public TaskCompletionSource<bool> InvocationCompletion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task WarmupAsync(CancellationToken cancellationToken)
        {
            InvocationCount++;
            InvocationCompletion.TrySetResult(true);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingDashboardSnapshotService : IDashboardSnapshotService
    {
        /// <summary>
        /// 获取看板快照刷新调用次数。
        /// </summary>
        public int InvocationCount { get; private set; }

        public Task RefreshAsync(CancellationToken ct)
        {
            InvocationCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingApiWarmupService : IApiWarmupService
    {
        public Task WarmupAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Warmup failed.");
        }
    }

    private sealed class TestHostApplicationLifetime : IHostApplicationLifetime
    {
        /// <summary>
        /// 存储已触发的应用启动令牌。
        /// </summary>
        private readonly CancellationToken _applicationStarted;

        public TestHostApplicationLifetime()
        {
            var startedCts = new CancellationTokenSource();
            startedCts.Cancel();
            _applicationStarted = startedCts.Token;
        }

        /// <summary>
        /// 获取应用已启动取消令牌。
        /// </summary>
        public CancellationToken ApplicationStarted => _applicationStarted;

        /// <summary>
        /// 获取应用停止中取消令牌。
        /// </summary>
        public CancellationToken ApplicationStopping => CancellationToken.None;

        /// <summary>
        /// 获取应用已停止取消令牌。
        /// </summary>
        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication()
        {
        }
    }

    private sealed class StubDatabaseConnectivityService : IDatabaseConnectivityService
    {
        /// <summary>
        /// 获取或设置本地 SQL Server 状态。
        /// </summary>
        public DatabaseEndpointConnectivityState LocalSqlServerState { get; set; } = CreateAvailableLocalSqlServerState();

        /// <summary>
        /// 获取或设置完整数据库连通性快照。
        /// </summary>
        public DatabaseConnectivitySnapshot Snapshot { get; set; } = new()
        {
            CheckedAtLocal = DateTime.Now,
            LocalSqlServer = CreateAvailableLocalSqlServerState(),
            Oracle = new DatabaseEndpointConnectivityState
            {
                DatabaseName = "Oracle",
                IsAvailable = true,
                Description = "Oracle available"
            }
        };

        /// <summary>
        /// 获取完整快照读取次数。
        /// </summary>
        public int GetSnapshotCount { get; private set; }

        /// <summary>
        /// 获取本地 SQL Server 快速状态读取次数。
        /// </summary>
        public int GetLocalSqlServerStateCount { get; private set; }

        public Task<DatabaseConnectivitySnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            GetSnapshotCount++;
            return Task.FromResult(Snapshot);
        }

        public Task<DatabaseConnectivitySnapshot> RefreshSnapshotAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Snapshot);
        }

        public Task<DatabaseEndpointConnectivityState> GetLocalSqlServerStateAsync(CancellationToken cancellationToken)
        {
            GetLocalSqlServerStateCount++;
            return Task.FromResult(LocalSqlServerState);
        }

        public Task<DatabaseEndpointConnectivityState> RefreshLocalSqlServerStateAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(LocalSqlServerState);
        }

        public bool IsDatabaseConnectivityException(Exception exception)
        {
            return false;
        }
    }
}
