using System.Diagnostics;
using System.Text;
using EverydayChain.Hub.Host.Middlewares.Streams;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EverydayChain.Hub.Host.Middlewares;

/// <summary>
/// 统一记录 API 失败请求与响应明细日志的中间件。
/// </summary>
public sealed class ApiFailureLoggingMiddleware {
    /// <summary>
    /// 单次日志允许写入的请求/响应文本最大长度。
    /// </summary>
    private const int MaxLoggedPayloadLength = 16384;

    /// <summary>
    /// UTF-8 单字符最大字节数。
    /// </summary>
    private const int Utf8WorstCaseBytesPerCharacter = 4;

    /// <summary>
    /// 响应捕获字节上限（按 UTF-8 最坏情况预估）。
    /// </summary>
    private const int MaxCapturedResponseBytes = MaxLoggedPayloadLength * Utf8WorstCaseBytesPerCharacter;

    /// <summary>
    /// 流读取缓冲区大小。
    /// </summary>
    private const int StreamReadBufferSize = 1024;

    /// <summary>
    /// 下一个请求委托。
    /// </summary>
    private readonly RequestDelegate next;

    /// <summary>
    /// 日志记录器。
    /// </summary>
    private readonly ILogger<ApiFailureLoggingMiddleware> logger;

    /// <summary>
    /// 初始化 API 失败日志中间件。
    /// </summary>
    /// <param name="next">下一个请求委托。</param>
    /// <param name="logger">日志记录器。</param>
    public ApiFailureLoggingMiddleware(RequestDelegate next, ILogger<ApiFailureLoggingMiddleware> logger) {
        this.next = next;
        this.logger = logger;
    }

    /// <summary>
    /// 执行请求处理并在失败场景记录详细日志。
    /// </summary>
    /// <param name="context">HTTP 上下文。</param>
    /// <returns>异步任务。</returns>
    public async Task InvokeAsync(HttpContext context) {
        if (!IsApiRequest(context.Request.Path)) {
            await next(context);
            return;
        }

        var requestBody = SanitizeForLog(await ReadRequestBodyAsync(context.Request, context.RequestAborted));
        var elapsedStopwatch = Stopwatch.StartNew();
        var requestPath = SanitizeForLog(context.Request.Path.Value ?? string.Empty);
        var queryString = SanitizeForLog(context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty);
        var userAgent = SanitizeForLog(context.Request.Headers.UserAgent.ToString());
        var originalResponseBody = context.Response.Body;
        using var responseCaptureStream = new BoundedCaptureWriteStream(originalResponseBody, MaxCapturedResponseBytes);
        context.Response.Body = responseCaptureStream;
        var hasUnhandledException = false;
        try {
            await next(context);
        }
        catch (Exception exception) {
            hasUnhandledException = true;
            var endpointName = SanitizeForLog(context.GetEndpoint()?.DisplayName ?? string.Empty);
            logger.LogError(
                exception,
                "API 请求处理异常。请求方式: {Method}; 路径: {Path}; 查询字符串: {QueryString}; TraceId: {TraceId}; Endpoint: {Endpoint}; 客户端: {UserAgent}; 耗时毫秒: {ElapsedMilliseconds}; 请求体: {RequestBody}",
                context.Request.Method,
                requestPath,
                queryString,
                context.TraceIdentifier,
                endpointName,
                userAgent,
                elapsedStopwatch.Elapsed.TotalMilliseconds,
                requestBody);
            throw;
        }
        finally {
            context.Response.Body = originalResponseBody;
            var endpointName = SanitizeForLog(context.GetEndpoint()?.DisplayName ?? string.Empty);
            var responseBody = SanitizeForLog(TruncatePayload(responseCaptureStream.GetCapturedText(Encoding.UTF8)));

            if (!hasUnhandledException && ShouldLogFailure(context.Response.StatusCode, responseBody)) {
                logger.LogError(
                    "API 请求响应失败。请求方式: {Method}; 路径: {Path}; 查询字符串: {QueryString}; 状态码: {StatusCode}; TraceId: {TraceId}; Endpoint: {Endpoint}; 客户端: {UserAgent}; 耗时毫秒: {ElapsedMilliseconds}; 请求体: {RequestBody}; 响应体: {ResponseBody}",
                    context.Request.Method,
                    requestPath,
                    queryString,
                    context.Response.StatusCode,
                    context.TraceIdentifier,
                    endpointName,
                    userAgent,
                    elapsedStopwatch.Elapsed.TotalMilliseconds,
                    requestBody,
                    responseBody);
            }
        }
    }

    /// <summary>
    /// 判断当前请求是否属于 API 端点。
    /// </summary>
    /// <param name="requestPath">请求路径。</param>
    /// <returns>属于 API 端点返回 true，否则返回 false。</returns>
    private static bool IsApiRequest(PathString requestPath) {
        return requestPath.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 判断响应是否属于失败场景。
    /// </summary>
    /// <param name="statusCode">HTTP 状态码。</param>
    /// <param name="responseBody">响应文本。</param>
    /// <returns>失败场景返回 true，否则返回 false。</returns>
    private static bool ShouldLogFailure(int statusCode, string responseBody) {
        if (statusCode >= StatusCodes.Status400BadRequest) {
            return true;
        }

        return ContainsBusinessFailureFlag(responseBody);
    }

    /// <summary>
    /// 从统一响应结构中识别业务失败标记。
    /// </summary>
    /// <param name="responseBody">响应文本。</param>
    /// <returns>识别为业务失败返回 true，否则返回 false。</returns>
    private static bool ContainsBusinessFailureFlag(string responseBody) {
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
    /// 读取响应对象中的业务成功标记。
    /// </summary>
    /// <param name="root">响应对象根节点。</param>
    /// <param name="isSuccess">业务成功标记。</param>
    /// <returns>读取成功返回 true，否则返回 false。</returns>
    private static bool TryReadIsSuccessProperty(JObject root, out bool isSuccess) {
        isSuccess = false;
        var value = root.GetValue("isSuccess", StringComparison.OrdinalIgnoreCase);
        if (value?.Type == JTokenType.Boolean) {
            isSuccess = value.Value<bool>();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 读取并保留请求体文本。
    /// </summary>
    /// <param name="request">HTTP 请求对象。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>请求体文本。</returns>
    private static async Task<string> ReadRequestBodyAsync(HttpRequest request, CancellationToken cancellationToken) {
        if (request.ContentLength == 0 || request.Body == Stream.Null) {
            return string.Empty;
        }

        request.EnableBuffering();
        if (!TryResetStreamPosition(request.Body, 0)) {
            return string.Empty;
        }

        var content = await ReadStreamAsync(request.Body, MaxLoggedPayloadLength + 1, cancellationToken);
        _ = TryResetStreamPosition(request.Body, 0);
        return TruncatePayload(content);
    }

    /// <summary>
    /// 从流中读取文本内容并执行长度截断。
    /// </summary>
    /// <param name="stream">目标流。</param>
    /// <param name="maxCharacters">最大读取字符数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>截断后的文本内容。</returns>
    private static async Task<string> ReadStreamAsync(Stream stream, int maxCharacters, CancellationToken cancellationToken) {
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
    /// 按固定上限截断日志载荷，避免单条日志过大。
    /// </summary>
    /// <param name="payload">原始载荷。</param>
    /// <returns>截断后的载荷。</returns>
    private static string TruncatePayload(string payload) {
        if (string.IsNullOrEmpty(payload) || payload.Length <= MaxLoggedPayloadLength) {
            return payload;
        }

        return $"{payload[..MaxLoggedPayloadLength]}...(已截断，原始长度={payload.Length})";
    }

    /// <summary>
    /// 规范化日志文本，避免换行注入影响日志结构。
    /// </summary>
    /// <param name="rawText">原始文本。</param>
    /// <returns>规范化后的文本。</returns>
    private static string SanitizeForLog(string rawText) {
        if (string.IsNullOrEmpty(rawText)) {
            return rawText;
        }

        return rawText.Replace("\r", "\\r", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal);
    }

    /// <summary>
    /// 重置流位置。
    /// </summary>
    /// <param name="stream">目标流。</param>
    /// <param name="position">目标位置。</param>
    /// <returns>重置成功返回 true，否则返回 false。</returns>
    private static bool TryResetStreamPosition(Stream stream, long position) {
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
