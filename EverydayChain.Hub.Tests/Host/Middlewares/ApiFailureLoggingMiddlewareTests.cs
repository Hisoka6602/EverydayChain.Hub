using System.Text;
using EverydayChain.Hub.Host.Middlewares;
using EverydayChain.Hub.Tests.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Tests.Host.Middlewares;

/// <summary>
/// 定义 ApiFailureLoggingMiddlewareTests 类型。
/// </summary>
public sealed class ApiFailureLoggingMiddlewareTests {
    /// <summary>
    /// 执行 InvokeAsync_ShouldLogError_WhenStatusCodeIsBadRequest 方法。
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ShouldLogError_WhenStatusCodeIsBadRequest() {
        // 步骤：执行 InvokeAsync_ShouldLogError_WhenStatusCodeIsBadRequest 方法的核心处理流程。
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
    /// 执行 InvokeAsync_ShouldLogError_WhenBusinessResponseIsFailure 方法。
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ShouldLogError_WhenBusinessResponseIsFailure() {
        // 步骤：执行 InvokeAsync_ShouldLogError_WhenBusinessResponseIsFailure 方法的核心处理流程。
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
    /// 执行 InvokeAsync_ShouldNotLogError_WhenResponseIsSuccessful 方法。
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ShouldNotLogError_WhenResponseIsSuccessful() {
        // 步骤：执行 InvokeAsync_ShouldNotLogError_WhenResponseIsSuccessful 方法的核心处理流程。
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
    /// 执行 InvokeAsync_ShouldLogExceptionAndRethrow_WhenRequestDelegateThrows 方法。
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ShouldLogExceptionAndRethrow_WhenRequestDelegateThrows() {
        // 步骤：执行 InvokeAsync_ShouldLogExceptionAndRethrow_WhenRequestDelegateThrows 方法的核心处理流程。
        var logger = new TestLogger<ApiFailureLoggingMiddleware>();
        var middleware = new ApiFailureLoggingMiddleware(_ => throw new InvalidOperationException("boom"), logger);
        var context = CreateContext("/api/v1/test", """{"waveCode":"WAVE-001"}""");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        Assert.Equal("boom", exception.Message);
        Assert.Contains(logger.Logs, entry => entry.Level == LogLevel.Error && entry.Message.Contains("API 请求处理异常", StringComparison.Ordinal));
    }

    /// <summary>
    /// 执行 InvokeAsync_ShouldLogRequestBody_WhenContentLengthIsNull 方法。
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ShouldLogRequestBody_WhenContentLengthIsNull() {
        // 步骤：执行 InvokeAsync_ShouldLogRequestBody_WhenContentLengthIsNull 方法的核心处理流程。
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
    /// 执行 CreateContext 方法。
    /// </summary>
    private static DefaultHttpContext CreateContext(string path, string requestBody, long? contentLength = null) {
        // 步骤：执行 CreateContext 方法的核心处理流程。
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
    /// 执行 ReadResponseBodyAsync 方法。
    /// </summary>
    private static async Task<string> ReadResponseBodyAsync(Stream responseBody) {
        // 步骤：执行 ReadResponseBodyAsync 方法的核心处理流程。
        responseBody.Position = 0;
        using var reader = new StreamReader(responseBody, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}

