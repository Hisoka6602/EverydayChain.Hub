using System.Diagnostics;
using System.Reflection;
using EverydayChain.Hub.Application.Abstractions.Infrastructure;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 数据库连通性服务测试。
/// </summary>
public sealed class DatabaseConnectivityServiceTests
{
    [Fact]
    public async Task GetSnapshotAsync_ShouldReuseFreshCachedSnapshot()
    {
        var service = CreateService();
        var cachedSnapshot = CreateAvailableSnapshot();
        SetFreshCachedSnapshot(service, cachedSnapshot);

        var snapshot = await service.GetSnapshotAsync(CancellationToken.None);

        Assert.Same(cachedSnapshot, snapshot);
        Assert.True(snapshot.LocalSqlServer.IsAvailable);
        Assert.True(snapshot.Oracle.IsAvailable);
    }

    [Fact]
    public async Task RefreshSnapshotAsync_ShouldBypassFreshCachedSnapshot()
    {
        var service = CreateService();
        var cachedSnapshot = CreateAvailableSnapshot();
        SetFreshCachedSnapshot(service, cachedSnapshot);

        var snapshot = await service.RefreshSnapshotAsync(CancellationToken.None);

        Assert.NotSame(cachedSnapshot, snapshot);
        Assert.False(snapshot.LocalSqlServer.IsAvailable);
        Assert.Contains("测试桩：禁止创建真实 DbContext。", snapshot.LocalSqlServer.Description, StringComparison.Ordinal);
        Assert.False(snapshot.Oracle.IsAvailable);
        Assert.Contains("Oracle.ConnectionString 不能为空。", snapshot.Oracle.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetLocalSqlServerStateAsync_ShouldReuseFreshCachedLocalSqlServerState()
    {
        var service = CreateService();
        var cachedState = CreateAvailableLocalSqlServerState();
        SetFreshCachedLocalSqlServerState(service, cachedState);

        var localSqlServerState = await service.GetLocalSqlServerStateAsync(CancellationToken.None);

        Assert.Same(cachedState, localSqlServerState);
        Assert.True(localSqlServerState.IsAvailable);
    }

    [Fact]
    public async Task RefreshLocalSqlServerStateAsync_ShouldProbeOnlyLocalSqlServer()
    {
        var service = CreateService();

        var localSqlServerState = await service.RefreshLocalSqlServerStateAsync(CancellationToken.None);

        Assert.False(localSqlServerState.IsAvailable);
        Assert.Contains("测试桩：禁止创建真实 DbContext。", localSqlServerState.Description, StringComparison.Ordinal);
    }

    private static DatabaseConnectivityService CreateService()
    {
        return new DatabaseConnectivityService(
            new ThrowingHubDbContextFactory(),
            Options.Create(new OracleOptions()),
            new TestLogger<DatabaseConnectivityService>());
    }

    private static DatabaseConnectivitySnapshot CreateAvailableSnapshot()
    {
        return new DatabaseConnectivitySnapshot
        {
            CheckedAtLocal = DateTime.Now,
            LocalSqlServer = CreateAvailableLocalSqlServerState(),
            Oracle = new DatabaseEndpointConnectivityState
            {
                DatabaseName = "远端 Oracle",
                IsAvailable = true,
                Description = "远端 Oracle 连接正常"
            }
        };
    }

    private static DatabaseEndpointConnectivityState CreateAvailableLocalSqlServerState()
    {
        return new DatabaseEndpointConnectivityState
        {
            DatabaseName = "本地 MSSQL",
            IsAvailable = true,
            Description = "本地 MSSQL 连接正常"
        };
    }

    private static void SetFreshCachedSnapshot(DatabaseConnectivityService service, DatabaseConnectivitySnapshot snapshot)
    {
        var serviceType = typeof(DatabaseConnectivityService);
        serviceType
            .GetField("_lastSnapshot", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(service, snapshot);
        serviceType
            .GetField("_lastProbeTimestamp", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(service, Stopwatch.GetTimestamp());
    }

    private static void SetFreshCachedLocalSqlServerState(DatabaseConnectivityService service, DatabaseEndpointConnectivityState localSqlServerState)
    {
        var serviceType = typeof(DatabaseConnectivityService);
        serviceType
            .GetField("_lastLocalSqlServerState", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(service, localSqlServerState);
        serviceType
            .GetField("_lastLocalSqlProbeTimestamp", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(service, Stopwatch.GetTimestamp());
    }
}
