using System.Text;
using EverydayChain.Hub.Application.Abstractions.Infrastructure;
using EverydayChain.Hub.Host.Middlewares;
using EverydayChain.Hub.Tests.Host.Workers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Host.Middlewares;

/// <summary>
/// 定义 DatabaseConnectivityMiddlewareTests 类型。
/// </summary>
public sealed class DatabaseConnectivityMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldReturnServiceUnavailable_WhenLocalSqlServerIsUnavailable()
    {
        var databaseConnectivityService = new StubDatabaseConnectivityService
        {
            Snapshot = CreateLocalSqlUnavailableSnapshot()
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
    }

    [Fact]
    public async Task InvokeAsync_ShouldAllowRequest_WhenOnlyOracleIsUnavailable()
    {
        var databaseConnectivityService = new StubDatabaseConnectivityService
        {
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
    }

    [Fact]
    public async Task InvokeAsync_ShouldTranslateConnectivityException_WhenRequestDelegateThrows()
    {
        var databaseConnectivityService = new StubDatabaseConnectivityService
        {
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
                Description = "本地 MSSQL 无法连接（连接探测超时）"
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

    /// <summary>
    /// 定义数据库连通性测试桩。
    /// </summary>
    private sealed class StubDatabaseConnectivityService : IDatabaseConnectivityService
    {
        /// <summary>
        /// 获取或设置当前快照。
        /// </summary>
        public DatabaseConnectivitySnapshot Snapshot { get; set; } = CreateAvailableSnapshot();

        /// <summary>
        /// 获取或设置刷新后的快照。
        /// </summary>
        public DatabaseConnectivitySnapshot? RefreshedSnapshot { get; set; }

        /// <summary>
        /// 获取或设置是否将异常视为连接异常。
        /// </summary>
        public bool TreatAllExceptionsAsConnectivityException { get; set; }

        /// <summary>
        /// 获取数据库连通性快照。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>数据库连通性快照。</returns>
        public Task<DatabaseConnectivitySnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Snapshot);
        }

        /// <summary>
        /// 强制刷新数据库连通性快照。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>数据库连通性快照。</returns>
        public Task<DatabaseConnectivitySnapshot> RefreshSnapshotAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(RefreshedSnapshot ?? Snapshot);
        }

        /// <summary>
        /// 判断异常是否属于数据库连接类异常。
        /// </summary>
        /// <param name="exception">待识别异常。</param>
        /// <returns>属于数据库连接类异常时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
        public bool IsDatabaseConnectivityException(Exception exception)
        {
            return TreatAllExceptionsAsConnectivityException;
        }
    }
}
