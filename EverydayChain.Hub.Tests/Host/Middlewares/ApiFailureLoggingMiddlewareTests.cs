using System.Text;
using EverydayChain.Hub.Host.Middlewares;
using EverydayChain.Hub.Tests.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Tests.Host.Middlewares;

/// <summary>
/// API 失败日志中间件测试。
/// </summary>
public sealed class ApiFailureLoggingMiddlewareTests {
    /// <summary>
    /// 非 2xx 响应应记录失败日志。
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ShouldLogError_WhenStatusCodeIsBadRequest() {
        var logger = new TestLogger<ApiFailureLoggingMiddleware>();
        var middleware = new ApiFailureLoggingMiddleware(async context => {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("""{"isSuccess":false,"message":"参数错误"}""");
        }, logger);
        var context = CreateContext("/api/v1/test", """{"barcode":"BC001"}""");

        await middleware.InvokeAsync(context);
        var responseBody = await ReadResponseBodyAsync(context.Response.Body);

        Assert.Equal("""{"isSuccess":false,"message":"参数错误"}""", responseBody);
        Assert.Contains(logger.Logs, entry => entry.Level == LogLevel.Error && entry.Message.Contains("状态码: 400", StringComparison.Ordinal));
        Assert.Contains(logger.Logs, entry => entry.Level == LogLevel.Error && entry.Message.Contains("""请求体: {"barcode":"BC001"}""", StringComparison.Ordinal));
    }

    /// <summary>
    /// HTTP 成功但业务失败时应记录失败日志。
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ShouldLogError_WhenBusinessResponseIsFailure() {
        var logger = new TestLogger<ApiFailureLoggingMiddleware>();
        var middleware = new ApiFailureLoggingMiddleware(async context => {
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsync("""{"isSuccess":false,"message":"业务失败"}""");
        }, logger);
        var context = CreateContext("/api/v1/test", """{"taskCode":"TASK-001"}""");

        await middleware.InvokeAsync(context);

        Assert.Contains(logger.Logs, entry => entry.Level == LogLevel.Error && entry.Message.Contains("API 请求响应失败", StringComparison.Ordinal));
    }

    /// <summary>
    /// HTTP 与业务同时成功时不应记录失败日志。
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ShouldNotLogError_WhenResponseIsSuccessful() {
        var logger = new TestLogger<ApiFailureLoggingMiddleware>();
        var middleware = new ApiFailureLoggingMiddleware(async context => {
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsync("""{"isSuccess":true,"message":"成功"}""");
        }, logger);
        var context = CreateContext("/api/v1/test", """{"traceId":"TRC-001"}""");

        await middleware.InvokeAsync(context);

        Assert.DoesNotContain(logger.Logs, entry => entry.Level == LogLevel.Error);
    }

    /// <summary>
    /// 请求处理抛出异常时应记录异常日志并继续抛出。
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ShouldLogExceptionAndRethrow_WhenRequestDelegateThrows() {
        var logger = new TestLogger<ApiFailureLoggingMiddleware>();
        var middleware = new ApiFailureLoggingMiddleware(_ => throw new InvalidOperationException("boom"), logger);
        var context = CreateContext("/api/v1/test", """{"waveCode":"WAVE-001"}""");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        Assert.Equal("boom", exception.Message);
        Assert.Contains(logger.Logs, entry => entry.Level == LogLevel.Error && entry.Message.Contains("API 请求处理异常", StringComparison.Ordinal));
    }

    /// <summary>
    /// ContentLength 为空时仍应读取并记录请求体。
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ShouldLogRequestBody_WhenContentLengthIsNull() {
        var logger = new TestLogger<ApiFailureLoggingMiddleware>();
        var middleware = new ApiFailureLoggingMiddleware(async context => {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("""{"isSuccess":false,"message":"失败"}""");
        }, logger);
        var context = CreateContext("/api/v1/test", """{"barcode":"BC-CHUNKED"}""", null);

        await middleware.InvokeAsync(context);

        Assert.Contains(logger.Logs, entry => entry.Level == LogLevel.Error && entry.Message.Contains("""请求体: {"barcode":"BC-CHUNKED"}""", StringComparison.Ordinal));
    }

    /// <summary>
    /// 构造用于中间件执行的 HttpContext。
    /// </summary>
    /// <param name="path">请求路径。</param>
    /// <param name="requestBody">请求体。</param>
    /// <param name="contentLength">请求体长度。</param>
    /// <returns>HTTP 上下文。</returns>
    private static DefaultHttpContext CreateContext(string path, string requestBody, long? contentLength = null) {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "TRACE-UNITTEST";
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = path;
        context.Request.QueryString = new QueryString("?source=unittest");
        context.Request.Headers.UserAgent = "UnitTest";
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        context.Request.ContentLength = contentLength ?? context.Request.Body.Length;
        context.Response.Body = new MemoryStream();
        return context;
    }

    /// <summary>
    /// 读取响应流文本。
    /// </summary>
    /// <param name="responseBody">响应流。</param>
    /// <returns>响应文本。</returns>
    private static async Task<string> ReadResponseBodyAsync(Stream responseBody) {
        responseBody.Position = 0;
        using var reader = new StreamReader(responseBody, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
