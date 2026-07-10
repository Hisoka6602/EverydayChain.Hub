using EverydayChain.Hub.Application.Abstractions.Infrastructure;
using EverydayChain.Hub.Host.Contracts.Responses;
using Newtonsoft.Json;

namespace EverydayChain.Hub.Host.Middlewares;

/// <summary>
/// 提供数据库连通性统一降级响应中间件。
/// </summary>
public sealed class DatabaseConnectivityMiddleware
{
    /// <summary>
    /// 存储后续请求委托。
    /// </summary>
    private readonly RequestDelegate _next;

    /// <summary>
    /// 存储数据库连通性服务。
    /// </summary>
    private readonly IDatabaseConnectivityService _databaseConnectivityService;

    /// <summary>
    /// 存储日志记录器。
    /// </summary>
    private readonly ILogger<DatabaseConnectivityMiddleware> _logger;

    /// <summary>
    /// 初始化数据库连通性中间件。
    /// </summary>
    /// <param name="next">后续请求委托。</param>
    /// <param name="databaseConnectivityService">数据库连通性服务。</param>
    /// <param name="logger">日志记录器。</param>
    public DatabaseConnectivityMiddleware(
        RequestDelegate next,
        IDatabaseConnectivityService databaseConnectivityService,
        ILogger<DatabaseConnectivityMiddleware> logger)
    {
        // 步骤：缓存依赖，供每次请求统一执行数据库降级判断。
        _next = next;
        _databaseConnectivityService = databaseConnectivityService;
        _logger = logger;
    }

    /// <summary>
    /// 处理当前请求。
    /// </summary>
    /// <param name="context">HTTP 上下文。</param>
    /// <returns>请求处理任务。</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // 步骤：仅拦截 API 请求，其他静态资源、根路径与 Swagger 页面不受影响。
        if (!IsApiRequest(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var localSqlServerState = await _databaseConnectivityService.GetLocalSqlServerStateAsync(context.RequestAborted);
        if (!localSqlServerState.IsAvailable)
        {
            await WriteUnavailableResponseAsync(
                context,
                $"数据库连接不可用：{localSqlServerState.Description}。",
                context.RequestAborted);
            return;
        }

        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (_databaseConnectivityService.IsDatabaseConnectivityException(ex) && !context.Response.HasStarted)
        {
            // 步骤：数据库在请求处理中途失联时刷新一次状态，并向前端返回统一友好文案。
            var refreshedSnapshot = await _databaseConnectivityService.RefreshSnapshotAsync(context.RequestAborted);
            var responseMessage = refreshedSnapshot.HasUnavailableDatabase
                ? refreshedSnapshot.BuildUserMessage()
                : "数据库连接不可用，请稍后重试。";
            _logger.LogWarning(
                ex,
                "API 请求处理中检测到数据库连接异常，已返回降级响应。Path={Path}",
                context.Request.Path.Value ?? string.Empty);
            await WriteUnavailableResponseAsync(context, responseMessage, context.RequestAborted);
        }
    }

    /// <summary>
    /// 判断当前路径是否为 API 请求。
    /// </summary>
    /// <param name="requestPath">请求路径。</param>
    /// <returns>是则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    private static bool IsApiRequest(PathString requestPath)
    {
        // 步骤：统一按 /api 前缀识别业务接口请求。
        return requestPath.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 输出数据库不可用响应。
    /// </summary>
    /// <param name="context">HTTP 上下文。</param>
    /// <param name="message">提示文案。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>写响应任务。</returns>
    private static async Task WriteUnavailableResponseAsync(
        HttpContext context,
        string message,
        CancellationToken cancellationToken)
    {
        // 步骤：统一返回 503 与标准 ApiResponse 包装，便于前端稳定处理降级场景。
        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/json; charset=utf-8";
        var payload = JsonConvert.SerializeObject(ApiResponse<object>.Fail(message));
        await context.Response.WriteAsync(payload, cancellationToken);
    }
}
