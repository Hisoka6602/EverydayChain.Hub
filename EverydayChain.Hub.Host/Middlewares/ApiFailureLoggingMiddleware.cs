using System.Diagnostics;
using System.Text;
using EverydayChain.Hub.Host.Middlewares.Streams;
using EverydayChain.Hub.Host.Startup;
using EverydayChain.Hub.SharedKernel.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EverydayChain.Hub.Host.Middlewares;

/// <summary>
/// 定义 ApiFailureLoggingMiddleware 类型。
/// </summary>
public sealed class ApiFailureLoggingMiddleware {
    /// <summary>
    /// 存储 MaxLoggedPayloadLength 字段。
    /// </summary>
    private const int MaxLoggedPayloadLength = 16384;

    /// <summary>
    /// 存储 Utf8WorstCaseBytesPerCharacter 字段。
    /// </summary>
    private const int Utf8WorstCaseBytesPerCharacter = 4;

    /// <summary>
    /// 存储 MaxCapturedResponseBytes 字段。
    /// </summary>
    private const int MaxCapturedResponseBytes = MaxLoggedPayloadLength * Utf8WorstCaseBytesPerCharacter;

    /// <summary>
    /// 存储 StreamReadBufferSize 字段。
    /// </summary>
    private const int StreamReadBufferSize = 1024;

    /// <summary>
    /// 存储 next 字段。
    /// </summary>
    private readonly RequestDelegate next;

    /// <summary>
    /// 存储 logger 字段。
    /// </summary>
    private readonly ILogger<ApiFailureLoggingMiddleware> logger;

    /// <summary>
    /// 执行 ApiFailureLoggingMiddleware 方法。
    /// </summary>
    public ApiFailureLoggingMiddleware(RequestDelegate next, ILogger<ApiFailureLoggingMiddleware> logger) {
        // 步骤：执行 ApiFailureLoggingMiddleware 方法的核心处理流程。
        this.next = next;
        this.logger = logger;
    }

    /// <summary>
    /// 执行 InvokeAsync 方法。
    /// </summary>
    public async Task InvokeAsync(HttpContext context) {
        // 步骤：执行 InvokeAsync 方法的核心处理流程。
        if (!IsApiRequest(context.Request.Path)) {
            await next(context);
            return;
        }

        var isInternalWarmupRequest = InternalWarmupRequestMarker.IsMarked(context.Request);
        var requestBody = LogTextUtility.EscapeLineBreaks(await ReadRequestBodyAsync(context.Request, context.RequestAborted));
        var elapsedStopwatch = Stopwatch.StartNew();
        var requestPath = LogTextUtility.EscapeLineBreaks(context.Request.Path.Value ?? string.Empty);
        var queryString = LogTextUtility.EscapeLineBreaks(context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty);
        var userAgent = LogTextUtility.EscapeLineBreaks(context.Request.Headers.UserAgent.ToString());
        var originalResponseBody = context.Response.Body;
        using var responseCaptureStream = new BoundedCaptureWriteStream(originalResponseBody, MaxCapturedResponseBytes);
        context.Response.Body = responseCaptureStream;
        var hasUnhandledException = false;
        try {
            await next(context);
        }
        catch (Exception exception) {
            hasUnhandledException = true;
            if (!isInternalWarmupRequest) {
                var endpointName = LogTextUtility.EscapeLineBreaks(context.GetEndpoint()?.DisplayName ?? string.Empty);
                logger.LogError(
                    exception,
                    "API 请求处理异常。请求方式: {Method}; 路径: {Path}; 查询字符串: {QueryString}; TraceId: {TraceId}; Endpoint: {Endpoint}; 客户端: {UserAgent}; 耗时毫秒: {ElapsedMilliseconds}; 请求体: {RequestBody}",
                    context.Request.Method,
                    requestPath,
                    queryString,
                    context.TraceIdentifier,
                    endpointName,
                    userAgent,
                    MetricDecimalUtility.ToMilliseconds(elapsedStopwatch.Elapsed),
                    requestBody);
            }
            throw;
        }
        finally {
            context.Response.Body = originalResponseBody;
            var endpointName = LogTextUtility.EscapeLineBreaks(context.GetEndpoint()?.DisplayName ?? string.Empty);
            var responseBody = LogTextUtility.EscapeLineBreaks(LogTextUtility.TruncateWithSuffix(responseCaptureStream.GetCapturedText(Encoding.UTF8), MaxLoggedPayloadLength));

            if (!isInternalWarmupRequest && !hasUnhandledException && ShouldLogFailure(context.Response.StatusCode, responseBody)) {
                logger.LogError(
                    "API 请求响应失败。请求方式: {Method}; 路径: {Path}; 查询字符串: {QueryString}; 状态码: {StatusCode}; TraceId: {TraceId}; Endpoint: {Endpoint}; 客户端: {UserAgent}; 耗时毫秒: {ElapsedMilliseconds}; 请求体: {RequestBody}; 响应体: {ResponseBody}",
                    context.Request.Method,
                    requestPath,
                    queryString,
                    context.Response.StatusCode,
                    context.TraceIdentifier,
                    endpointName,
                    userAgent,
                    MetricDecimalUtility.ToMilliseconds(elapsedStopwatch.Elapsed),
                    requestBody,
                    responseBody);
            }
        }
    }

    /// <summary>
    /// 执行 IsApiRequest 方法。
    /// </summary>
    private static bool IsApiRequest(PathString requestPath) {
        // 步骤：执行 IsApiRequest 方法的核心处理流程。
        return requestPath.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 执行 ShouldLogFailure 方法。
    /// </summary>
    private static bool ShouldLogFailure(int statusCode, string responseBody) {
        // 步骤：执行 ShouldLogFailure 方法的核心处理流程。
        if (statusCode >= StatusCodes.Status400BadRequest) {
            return true;
        }

        return ContainsBusinessFailureFlag(responseBody);
    }

    /// <summary>
    /// 执行 ContainsBusinessFailureFlag 方法。
    /// </summary>
    private static bool ContainsBusinessFailureFlag(string responseBody) {
        // 步骤：执行 ContainsBusinessFailureFlag 方法的核心处理流程。
        if (string.IsNullOrWhiteSpace(responseBody)) {
            return false;
        }

        try {
            var rootToken = JsonConvert.DeserializeObject<JToken>(responseBody);
            if (rootToken is not JObject rootObject) {
                return false;
            }

            if (!TryReadIsSuccessProperty(rootObject, out var isSuccess)) {
                return false;
            }

            return !isSuccess;
        }
        catch (JsonException) {
            return false;
        }
    }

    /// <summary>
    /// 执行 TryReadIsSuccessProperty 方法。
    /// </summary>
    private static bool TryReadIsSuccessProperty(JObject root, out bool isSuccess) {
        // 步骤：执行 TryReadIsSuccessProperty 方法的核心处理流程。
        isSuccess = false;
        var value = root.GetValue("isSuccess", StringComparison.OrdinalIgnoreCase);
        if (value?.Type == JTokenType.Boolean) {
            isSuccess = value.Value<bool>();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 执行 ReadRequestBodyAsync 方法。
    /// </summary>
    private static async Task<string> ReadRequestBodyAsync(HttpRequest request, CancellationToken cancellationToken) {
        // 步骤：执行 ReadRequestBodyAsync 方法的核心处理流程。
        if (request.ContentLength == 0 || request.Body == Stream.Null) {
            return string.Empty;
        }

        request.EnableBuffering();
        if (!TryResetStreamPosition(request.Body, 0)) {
            return string.Empty;
        }

        var content = await ReadStreamAsync(request.Body, MaxLoggedPayloadLength + 1, cancellationToken);
        _ = TryResetStreamPosition(request.Body, 0);
        return LogTextUtility.TruncateWithSuffix(content, MaxLoggedPayloadLength);
    }

    /// <summary>
    /// 执行 ReadStreamAsync 方法。
    /// </summary>
    private static async Task<string> ReadStreamAsync(Stream stream, int maxCharacters, CancellationToken cancellationToken) {
        // 步骤：执行 ReadStreamAsync 方法的核心处理流程。
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var buffer = new char[StreamReadBufferSize];
        var builder = new StringBuilder();
        var remainingCharacters = maxCharacters;
        while (remainingCharacters > 0) {
            var remaining = Math.Min(buffer.Length, remainingCharacters);
            var readCount = await reader.ReadAsync(buffer.AsMemory(0, remaining), cancellationToken);
            if (readCount == 0) {
                break;
            }

            builder.Append(buffer, 0, readCount);
            remainingCharacters -= readCount;
        }

        return builder.ToString();
    }

    /// <summary>
    /// 执行 TryResetStreamPosition 方法。
    /// </summary>
    private static bool TryResetStreamPosition(Stream stream, long position) {
        // 步骤：执行 TryResetStreamPosition 方法的核心处理流程。
        if (!stream.CanSeek) {
            return false;
        }

        try {
            stream.Position = position;
            return true;
        }
        catch (IOException) {
            return false;
        }
        catch (NotSupportedException) {
            return false;
        }
    }
}

