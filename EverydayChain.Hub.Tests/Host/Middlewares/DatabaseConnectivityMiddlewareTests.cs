using System.Text;
using EverydayChain.Hub.Application.Abstractions.Infrastructure;
using EverydayChain.Hub.Host.Middlewares;
using EverydayChain.Hub.Tests.Host.Workers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Host.Middlewares;

/// <summary>
/// 数据库连通性中间件测试。
/// </summary>
public sealed class DatabaseConnectivityMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldReturnServiceUnavailable_WhenLocalSqlServerIsUnavailable()
    {
        var databaseConnectivityService = new StubDatabaseConnectivityService
        {
            LocalSqlServerState = CreateUnavailableLocalSqlServerState()
        };
        var nextCalled = false;
        var middleware = new DatabaseConnectivityMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            databaseConnectivityService,
            NullLogger<DatabaseConnectivityMiddleware>.Instance);
        var context = CreateContext("/api/v1/test");

        await middleware.InvokeAsync(context);
        var responseBody = await ReadResponseBodyAsync(context.Response.Body);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        Assert.Contains("数据库连接不可用", responseBody, StringComparison.Ordinal);
        Assert.Contains("本地 MSSQL 无法连接", responseBody, StringComparison.Ordinal);
        Assert.Equal(1, databaseConnectivityService.GetLocalSqlServerStateCount);
        Assert.Equal(0, databaseConnectivityService.GetSnapshotCount);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAllowRequest_WhenOnlyOracleIsUnavailable()
    {
        var databaseConnectivityService = new StubDatabaseConnectivityService
        {
            LocalSqlServerState = CreateAvailableLocalSqlServerState(),
            Snapshot = CreateOracleUnavailableSnapshot()
        };
        var nextCalled = false;
        var middleware = new DatabaseConnectivityMiddleware(
            httpContext =>
            {
                nextCalled = true;
                httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            },
            databaseConnectivityService,
            NullLogger<DatabaseConnectivityMiddleware>.Instance);
        var context = CreateContext("/api/v1/test");

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        Assert.Equal(1, databaseConnectivityService.GetLocalSqlServerStateCount);
        Assert.Equal(0, databaseConnectivityService.GetSnapshotCount);
    }

    [Fact]
    public async Task InvokeAsync_ShouldTranslateConnectivityException_WhenRequestDelegateThrows()
    {
        var databaseConnectivityService = new StubDatabaseConnectivityService
        {
            LocalSqlServerState = CreateAvailableLocalSqlServerState(),
            Snapshot = CreateAvailableSnapshot(),
            RefreshedSnapshot = CreateOracleUnavailableSnapshot(),
            TreatAllExceptionsAsConnectivityException = true
        };
        var middleware = new DatabaseConnectivityMiddleware(
            _ => throw new TestDatabaseException("测试桩：数据库连接失败。", isTransient: true),
            databaseConnectivityService,
            NullLogger<DatabaseConnectivityMiddleware>.Instance);
        var context = CreateContext("/api/v1/test");

        await middleware.InvokeAsync(context);
        var responseBody = await ReadResponseBodyAsync(context.Response.Body);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        Assert.Contains("远端 Oracle 无法连接", responseBody, StringComparison.Ordinal);
        Assert.Equal(1, databaseConnectivityService.GetLocalSqlServerStateCount);
        Assert.Equal(1, databaseConnectivityService.RefreshSnapshotCount);
    }

    private static DefaultHttpContext CreateContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadResponseBodyAsync(Stream responseBody)
    {
        responseBody.Position = 0;
        using var reader = new StreamReader(responseBody, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        return await reader.ReadToEndAsync();
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

    private static DatabaseConnectivitySnapshot CreateOracleUnavailableSnapshot()
    {
        return new DatabaseConnectivitySnapshot
        {
            CheckedAtLocal = DateTime.Now,
            LocalSqlServer = CreateAvailableLocalSqlServerState(),
            Oracle = new DatabaseEndpointConnectivityState
            {
                DatabaseName = "远端 Oracle",
                IsAvailable = false,
                Description = "远端 Oracle 无法连接（监听未注册）"
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

    private static DatabaseEndpointConnectivityState CreateUnavailableLocalSqlServerState()
    {
        return new DatabaseEndpointConnectivityState
        {
            DatabaseName = "本地 MSSQL",
            IsAvailable = false,
            Description = "本地 MSSQL 无法连接（连接探测超时）"
        };
    }

    /// <summary>
    /// 数据库连通性测试桩。
    /// </summary>
    private sealed class StubDatabaseConnectivityService : IDatabaseConnectivityService
    {
        /// <summary>
        /// 获取或设置快速本地 MSSQL 状态。
        /// </summary>
        public DatabaseEndpointConnectivityState LocalSqlServerState { get; set; } = CreateAvailableLocalSqlServerState();

        /// <summary>
        /// 获取或设置当前返回的数据库连通性快照。
        /// </summary>
        public DatabaseConnectivitySnapshot Snapshot { get; set; } = CreateAvailableSnapshot();

        /// <summary>
        /// 获取或设置刷新后的数据库连通性快照。
        /// </summary>
        public DatabaseConnectivitySnapshot? RefreshedSnapshot { get; set; }

        /// <summary>
        /// 获取或设置是否将异常视为连接异常。
        /// </summary>
        public bool TreatAllExceptionsAsConnectivityException { get; set; }

        /// <summary>
        /// 获取完整快照次数。
        /// </summary>
        public int GetSnapshotCount { get; private set; }

        /// <summary>
        /// 获取本地 MSSQL 快速状态次数。
        /// </summary>
        public int GetLocalSqlServerStateCount { get; private set; }

        /// <summary>
        /// 获取完整快照刷新次数。
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
            return Task.FromResult(RefreshedSnapshot ?? Snapshot);
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
            return TreatAllExceptionsAsConnectivityException;
        }
    }
}
