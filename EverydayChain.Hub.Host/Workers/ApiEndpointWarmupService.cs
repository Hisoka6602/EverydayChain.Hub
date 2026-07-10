using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Host.Startup;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Host.Workers;

/// <summary>
/// 定义 HTTP 端点预热服务。
/// </summary>
public interface IApiEndpointWarmupService
{
    /// <summary>
    /// 预热关键 HTTP 端点。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    Task WarmupAsync(CancellationToken cancellationToken);
}

/// <summary>
/// 通过本机回环调用预热 HTTP 端点，降低首次真实请求的路由、模型绑定、序列化与查询链路冷启动开销。
/// </summary>
public sealed class ApiEndpointWarmupService : IApiEndpointWarmupService
{
    /// <summary>
    /// 存储默认预热回看小时数。
    /// </summary>
    private const int WarmupLookbackHours = 24;

    /// <summary>
    /// 存储预热分页大小。
    /// </summary>
    private const int WarmupPageSize = 100;

    /// <summary>
    /// 存储兜底预热波次号。
    /// </summary>
    private const string WarmupWaveCode = "WARMUP";

    /// <summary>
    /// 存储兜底预热条码。
    /// </summary>
    private const string WarmupBarcode = "WARMUP-BARCODE";

    /// <summary>
    /// 存储兜底预热任务号。
    /// </summary>
    private const string WarmupTaskCode = "WARMUP-TASK";

    /// <summary>
    /// 存储兜底预热格口编码。
    /// </summary>
    private const string WarmupChuteCode = "WARMUP-CHUTE";

    /// <summary>
    /// 存储兜底预热设备编码。
    /// </summary>
    private const string WarmupDeviceCode = "WARMUP-DEVICE";

    /// <summary>
    /// 存储预热使用的目标表名。
    /// </summary>
    private const string WarmupTargetTableName = "business_tasks";

    /// <summary>
    /// 存储安全未命中预热使用的任务号。
    /// </summary>
    private const string WarmupUnknownTaskCode = "WARMUP-TASK-NOT-FOUND";

    /// <summary>
    /// 存储安全未命中预热使用的条码。
    /// </summary>
    private const string WarmupUnknownBarcode = "WARMUP-BARCODE-NOT-FOUND";

    /// <summary>
    /// 存储本机回环调用使用的 HTTP 客户端。
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// 存储日志记录器。
    /// </summary>
    private readonly ILogger<ApiEndpointWarmupService> _logger;

    /// <summary>
    /// 初始化 HTTP 端点预热服务。
    /// </summary>
    /// <param name="httpClient">本机回环调用使用的 HTTP 客户端。</param>
    /// <param name="webEndpointOptions">Web 端点配置。</param>
    /// <param name="logger">日志记录器。</param>
    public ApiEndpointWarmupService(
        HttpClient httpClient,
        IOptions<WebEndpointOptions> webEndpointOptions,
        ILogger<ApiEndpointWarmupService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress ??= ResolveBaseAddress(webEndpointOptions.Value.Url);
        if (!_httpClient.DefaultRequestHeaders.Contains(InternalWarmupRequestMarker.HeaderName))
        {
            _httpClient.DefaultRequestHeaders.Add(InternalWarmupRequestMarker.HeaderName, InternalWarmupRequestMarker.HeaderValue);
        }
    }

    /// <summary>
    /// 预热关键 HTTP 端点。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    public async Task WarmupAsync(CancellationToken cancellationToken)
    {
        var now = TruncateToSecond(DateTime.Now);
        var startTimeLocal = now.AddHours(-WarmupLookbackHours);
        var endTimeLocal = now;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "启动 HTTP 端点预热开始。BaseAddress={BaseAddress}, StartTimeLocal={StartTimeLocal}, EndTimeLocal={EndTimeLocal}",
            _httpClient.BaseAddress,
            startTimeLocal,
            endTimeLocal);

        await WarmupHostEndpointsAsync(cancellationToken);

        var discovery = await DiscoverWarmupContextAsync(startTimeLocal, endTimeLocal, cancellationToken);

        _logger.LogInformation(
            "启动 HTTP 端点预热已发现可复用上下文。WaveCode={WaveCode}, TaskCode={TaskCode}, Barcode={Barcode}, OrderId={OrderId}",
            discovery.WaveCode,
            discovery.TaskCode,
            discovery.Barcode,
            discovery.OrderId ?? string.Empty);

        await WarmupQueryAndExportEndpointsAsync(startTimeLocal, endTimeLocal, discovery, cancellationToken);
        await WarmupMutationEndpointsAsync(now, discovery, cancellationToken);

        stopwatch.Stop();
        _logger.LogInformation(
            "启动 HTTP 端点预热完成。BaseAddress={BaseAddress}, ElapsedMilliseconds={ElapsedMilliseconds}",
            _httpClient.BaseAddress,
            stopwatch.ElapsedMilliseconds);
    }

    private async Task WarmupHostEndpointsAsync(CancellationToken cancellationToken)
    {
        await WarmupGetEndpointAsync(
            "宿主根路径",
            "/",
            "RouteOnly",
            [HttpStatusCode.OK],
            cancellationToken);
        await WarmupGetEndpointAsync(
            "存活探针",
            "/health/live",
            "RouteOnly",
            [HttpStatusCode.OK],
            cancellationToken);
        await WarmupGetEndpointAsync(
            "就绪探针",
            "/health/ready",
            "RouteOnly",
            [HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable],
            cancellationToken);
    }

    private async Task<WarmupDiscoveryContext> DiscoverWarmupContextAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        CancellationToken cancellationToken)
    {
        using var currentWaveDocument = await WarmupJsonEndpointAsync(
            "波次当前查询端点",
            "api/v1/waves/current",
            new
            {
                startTimeLocal,
                endTimeLocal
            },
            "ReadOnly",
            [HttpStatusCode.OK],
            cancellationToken);
        using var waveOptionsDocument = await WarmupJsonEndpointAsync(
            "波次选项查询端点",
            "api/v1/waves/options",
            new
            {
                startTimeLocal,
                endTimeLocal
            },
            "ReadOnly",
            [HttpStatusCode.OK],
            cancellationToken);
        using var waveListDocument = await WarmupJsonEndpointAsync(
            "波次列表查询端点",
            "api/v1/waves/list",
            new
            {
                startTimeLocal,
                endTimeLocal
            },
            "ReadOnly",
            [HttpStatusCode.OK],
            cancellationToken);
        using var businessTasksDocument = await WarmupJsonEndpointAsync(
            "业务任务查询端点",
            "api/v1/business-query/tasks",
            new
            {
                startTimeLocal,
                endTimeLocal,
                pageNumber = 1,
                pageSize = WarmupPageSize
            },
            "ReadOnly",
            [HttpStatusCode.OK],
            cancellationToken);

        var discoveredWaveCode =
            TryReadString(currentWaveDocument, "data", "waveCode")
            ?? TryReadString(waveOptionsDocument, "data", "waveOptions", 0, "waveCode")
            ?? TryReadString(waveListDocument, "data", "items", 0, "waveId")
            ?? TryReadString(businessTasksDocument, "data", "items", 0, "waveCode")
            ?? WarmupWaveCode;
        var discoveredTaskCode =
            TryReadString(businessTasksDocument, "data", "items", 0, "taskCode")
            ?? WarmupTaskCode;
        var discoveredBarcode =
            TryReadString(businessTasksDocument, "data", "items", 0, "barcode")
            ?? TryReadString(currentWaveDocument, "data", "barcode")
            ?? WarmupBarcode;
        var discoveredOrderId =
            TryReadString(businessTasksDocument, "data", "items", 0, "orderId");

        return new WarmupDiscoveryContext(
            discoveredWaveCode,
            discoveredTaskCode,
            discoveredBarcode,
            discoveredOrderId);
    }

    private async Task WarmupQueryAndExportEndpointsAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        WarmupDiscoveryContext discovery,
        CancellationToken cancellationToken)
    {
        await WarmupJsonEndpointAsync(
            "总看板查询端点",
            "api/v1/dashboard/overview",
            new
            {
                startTimeLocal,
                endTimeLocal
            },
            "ReadOnly",
            [HttpStatusCode.OK],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "总看板 CSV 导出端点",
            "api/v1/dashboard/export/csv",
            new
            {
                startTimeLocal,
                endTimeLocal
            },
            "ReadOnlyExport",
            [HttpStatusCode.OK],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "总看板 Excel 导出端点",
            "api/v1/dashboard/export/xlsx",
            new
            {
                startTimeLocal,
                endTimeLocal
            },
            "ReadOnlyExport",
            [HttpStatusCode.OK],
            cancellationToken);

        await WarmupJsonEndpointAsync(
            "码头看板查询端点（自动波次）",
            "api/v1/dock-dashboard/overview",
            new
            {
                startTimeLocal,
                endTimeLocal
            },
            "ReadOnly",
            [HttpStatusCode.OK],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "码头看板查询端点（指定波次）",
            "api/v1/dock-dashboard/overview",
            new
            {
                startTimeLocal,
                endTimeLocal,
                waveCode = discovery.WaveCode
            },
            "ReadOnly",
            [HttpStatusCode.OK],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "码头看板 CSV 导出端点",
            "api/v1/dock-dashboard/export/csv",
            new
            {
                startTimeLocal,
                endTimeLocal,
                waveCode = discovery.WaveCode
            },
            "ReadOnlyExport",
            [HttpStatusCode.OK],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "码头看板 Excel 导出端点",
            "api/v1/dock-dashboard/export/xlsx",
            new
            {
                startTimeLocal,
                endTimeLocal,
                waveCode = discovery.WaveCode
            },
            "ReadOnlyExport",
            [HttpStatusCode.OK],
            cancellationToken);

        await WarmupJsonEndpointAsync(
            "波次汇总查询端点",
            "api/v1/waves/summary",
            new
            {
                startTimeLocal,
                endTimeLocal,
                waveCode = discovery.WaveCode
            },
            "ReadOnly",
            [HttpStatusCode.OK, HttpStatusCode.BadRequest],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "波次分区查询端点",
            "api/v1/waves/zones",
            new
            {
                startTimeLocal,
                endTimeLocal,
                waveCode = discovery.WaveCode
            },
            "ReadOnly",
            [HttpStatusCode.OK, HttpStatusCode.BadRequest],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "波次分区 CSV 导出端点",
            "api/v1/waves/zones/export/csv",
            new
            {
                startTimeLocal,
                endTimeLocal,
                waveCode = discovery.WaveCode
            },
            "ReadOnlyExport",
            [HttpStatusCode.OK, HttpStatusCode.BadRequest],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "波次分区 Excel 导出端点",
            "api/v1/waves/zones/export/xlsx",
            new
            {
                startTimeLocal,
                endTimeLocal,
                waveCode = discovery.WaveCode
            },
            "ReadOnlyExport",
            [HttpStatusCode.OK, HttpStatusCode.BadRequest],
            cancellationToken);

        await WarmupJsonEndpointAsync(
            "波次明细查询端点",
            "api/v1/waves/details",
            new
            {
                startTimeLocal,
                endTimeLocal,
                waveCode = discovery.WaveCode
            },
            "ReadOnly",
            [HttpStatusCode.OK],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "波次明细 CSV 导出端点",
            "api/v1/waves/details/export/csv",
            new
            {
                startTimeLocal,
                endTimeLocal,
                waveCode = discovery.WaveCode
            },
            "ReadOnlyExport",
            [HttpStatusCode.OK],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "波次明细 Excel 导出端点",
            "api/v1/waves/details/export/xlsx",
            new
            {
                startTimeLocal,
                endTimeLocal,
                waveCode = discovery.WaveCode
            },
            "ReadOnlyExport",
            [HttpStatusCode.OK],
            cancellationToken);

        await WarmupJsonEndpointAsync(
            "波次列表 CSV 导出端点",
            "api/v1/waves/list/export/csv",
            new
            {
                startTimeLocal,
                endTimeLocal
            },
            "ReadOnlyExport",
            [HttpStatusCode.OK],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "波次列表 Excel 导出端点",
            "api/v1/waves/list/export/xlsx",
            new
            {
                startTimeLocal,
                endTimeLocal
            },
            "ReadOnlyExport",
            [HttpStatusCode.OK],
            cancellationToken);

        await WarmupJsonEndpointAsync(
            "波次清理查询端点",
            "api/v1/wave-cleanup/query",
            new
            {
                waveCode = discovery.WaveCode
            },
            "ReadOnly",
            [HttpStatusCode.OK],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "波次清理 DryRun 端点",
            "api/v1/wave-cleanup/dry-run",
            new
            {
                waveCode = discovery.WaveCode
            },
            "SafeDryRun",
            [HttpStatusCode.OK],
            cancellationToken);

        await WarmupJsonEndpointAsync(
            "业务异常查询端点",
            "api/v1/business-query/exceptions",
            new
            {
                startTimeLocal,
                endTimeLocal,
                pageNumber = 1,
                pageSize = WarmupPageSize
            },
            "ReadOnly",
            [HttpStatusCode.OK],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "业务回流查询端点",
            "api/v1/business-query/recirculations",
            new
            {
                startTimeLocal,
                endTimeLocal,
                pageNumber = 1,
                pageSize = WarmupPageSize
            },
            "ReadOnly",
            [HttpStatusCode.OK],
            cancellationToken);

        await WarmupJsonEndpointAsync(
            "回流汇总查询端点",
            "api/v1/recirculations/summary",
            new
            {
                startTimeLocal,
                endTimeLocal,
                sortOrder = "Most"
            },
            "ReadOnly",
            [HttpStatusCode.OK],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "回流汇总 CSV 导出端点",
            "api/v1/recirculations/summary/export/csv",
            new
            {
                startTimeLocal,
                endTimeLocal,
                sortOrder = "Most"
            },
            "ReadOnlyExport",
            [HttpStatusCode.OK],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "回流汇总 Excel 导出端点",
            "api/v1/recirculations/summary/export/xlsx",
            new
            {
                startTimeLocal,
                endTimeLocal,
                sortOrder = "Most"
            },
            "ReadOnlyExport",
            [HttpStatusCode.OK],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "回流明细查询端点",
            "api/v1/recirculations/details",
            new
            {
                startTimeLocal,
                endTimeLocal,
                pageNumber = 1,
                pageSize = WarmupPageSize
            },
            "ReadOnly",
            [HttpStatusCode.OK],
            cancellationToken);

        await WarmupJsonEndpointAsync(
            "分拣报表查询端点",
            "api/v1/reports/query",
            new
            {
                startTimeLocal,
                endTimeLocal
            },
            "ReadOnly",
            [HttpStatusCode.OK],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "分拣报表 CSV 导出端点",
            "api/v1/reports/export/csv",
            new
            {
                startTimeLocal,
                endTimeLocal
            },
            "ReadOnlyExport",
            [HttpStatusCode.OK],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "分拣报表 Excel 导出端点",
            "api/v1/reports/export/xlsx",
            new
            {
                startTimeLocal,
                endTimeLocal
            },
            "ReadOnlyExport",
            [HttpStatusCode.OK],
            cancellationToken);

        await WarmupJsonEndpointAsync(
            "箱号追踪查询端点",
            "api/v1/box-tracking/query",
            new
            {
                startTimeLocal,
                endTimeLocal,
                boxId = discovery.Barcode,
                orderId = discovery.OrderId,
                pageNumber = 1,
                pageSize = WarmupPageSize
            },
            "ReadOnly",
            [HttpStatusCode.OK],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "箱号追踪 CSV 导出端点",
            "api/v1/box-tracking/export/csv",
            new
            {
                startTimeLocal,
                endTimeLocal,
                boxId = discovery.Barcode,
                orderId = discovery.OrderId
            },
            "ReadOnlyExport",
            [HttpStatusCode.OK],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "箱号追踪 Excel 导出端点",
            "api/v1/box-tracking/export/xlsx",
            new
            {
                startTimeLocal,
                endTimeLocal,
                boxId = discovery.Barcode,
                orderId = discovery.OrderId
            },
            "ReadOnlyExport",
            [HttpStatusCode.OK],
            cancellationToken);

        await WarmupJsonEndpointAsync(
            "格口解析查询端点",
            "api/v1/chute/resolve",
            new
            {
                taskCode = discovery.TaskCode,
                barcode = discovery.Barcode
            },
            "ReadOnly",
            [HttpStatusCode.OK],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "导出目录查询端点",
            "api/v1/exports/catalog",
            new
            {
                startTimeLocal,
                endTimeLocal
            },
            "ReadOnly",
            [HttpStatusCode.OK],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "保留期清理审计查询端点",
            "api/v1/retention-cleanups/query",
            new
            {
                startTimeLocal,
                endTimeLocal,
                pageNumber = 1,
                pageSize = WarmupPageSize
            },
            "ReadOnly",
            [HttpStatusCode.OK],
            cancellationToken);
    }

    private async Task WarmupMutationEndpointsAsync(
        DateTime now,
        WarmupDiscoveryContext discovery,
        CancellationToken cancellationToken)
    {
        await WarmupJsonEndpointAsync(
            "业务任务补数端点（安全失败预热）",
            "api/v1/business-task-seed/manual",
            new
            {
                targetTableName = WarmupTargetTableName,
                barcodes = Array.Empty<string>()
            },
            "SafeServiceFailure",
            [HttpStatusCode.BadRequest],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "落格回传端点（安全未命中预热）",
            "api/v1/drop-feedback/confirm",
            new
            {
                taskCode = WarmupUnknownTaskCode,
                barcode = WarmupUnknownBarcode,
                actualChuteCode = WarmupChuteCode,
                dropTimeLocal = now,
                isSuccess = true
            },
            "SafeServiceMiss",
            [HttpStatusCode.OK],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "扫描上传端点（安全校验预热）",
            "api/v1/scan/upload",
            new
            {
                barcodes = Array.Empty<string>(),
                deviceCode = WarmupDeviceCode,
                scanTimeLocal = now,
                traceId = "WARMUP-TRACE"
            },
            "ValidationOnly",
            [HttpStatusCode.BadRequest],
            cancellationToken);
        await WarmupRawEndpointAsync(
            "手工同步端点（安全校验预热）",
            "api/v1/dashboard/sync",
            "{",
            "application/json",
            "ValidationOnly",
            [HttpStatusCode.BadRequest],
            cancellationToken);
        await WarmupJsonEndpointAsync(
            "波次正式清理端点（安全校验预热）",
            "api/v1/wave-cleanup/execute",
            new
            {
                waveCode = string.Empty
            },
            "ValidationOnly",
            [HttpStatusCode.BadRequest],
            cancellationToken);
    }

    private async Task<JsonDocument?> WarmupGetEndpointAsync(
        string endpointName,
        string relativeUrl,
        string warmupMode,
        IReadOnlyCollection<HttpStatusCode> expectedStatusCodes,
        CancellationToken cancellationToken)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        return await SendWarmupRequestAsync(endpointName, requestMessage, relativeUrl, warmupMode, expectedStatusCodes, cancellationToken);
    }

    private async Task<JsonDocument?> WarmupJsonEndpointAsync(
        string endpointName,
        string relativeUrl,
        object requestBody,
        string warmupMode,
        IReadOnlyCollection<HttpStatusCode> expectedStatusCodes,
        CancellationToken cancellationToken)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, relativeUrl)
        {
            Content = JsonContent.Create(requestBody)
        };
        return await SendWarmupRequestAsync(endpointName, requestMessage, relativeUrl, warmupMode, expectedStatusCodes, cancellationToken);
    }

    private async Task<JsonDocument?> WarmupRawEndpointAsync(
        string endpointName,
        string relativeUrl,
        string rawRequestBody,
        string mediaType,
        string warmupMode,
        IReadOnlyCollection<HttpStatusCode> expectedStatusCodes,
        CancellationToken cancellationToken)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, relativeUrl)
        {
            Content = new StringContent(rawRequestBody, Encoding.UTF8, mediaType)
        };
        return await SendWarmupRequestAsync(endpointName, requestMessage, relativeUrl, warmupMode, expectedStatusCodes, cancellationToken);
    }

    private async Task<JsonDocument?> SendWarmupRequestAsync(
        string endpointName,
        HttpRequestMessage requestMessage,
        string relativeUrl,
        string warmupMode,
        IReadOnlyCollection<HttpStatusCode> expectedStatusCodes,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            var responseBody = await ReadResponseBodyAsync(response, cancellationToken);
            var responseMediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            stopwatch.Stop();

            var parsedBody = TryParseJson(responseBody, responseMediaType);
            var apiSuccess = TryReadIsSuccess(parsedBody);
            var apiMessage = TryReadString(parsedBody, "message");
            var hasExpectedStatusCode = expectedStatusCodes.Contains(response.StatusCode);
            var hasUnexpectedBusinessFailure = response.IsSuccessStatusCode && apiSuccess is false;

            if (hasExpectedStatusCode && !hasUnexpectedBusinessFailure)
            {
                _logger.LogInformation(
                    "启动 HTTP 端点预热完成。BaseAddress={BaseAddress}, EndpointName={EndpointName}, WarmupMode={WarmupMode}, RelativeUrl={RelativeUrl}, StatusCode={StatusCode}, ApiSuccess={ApiSuccess}, ElapsedMilliseconds={ElapsedMilliseconds}, Message={Message}",
                    _httpClient.BaseAddress,
                    endpointName,
                    warmupMode,
                    relativeUrl,
                    (int)response.StatusCode,
                    apiSuccess,
                    stopwatch.ElapsedMilliseconds,
                    apiMessage ?? string.Empty);
            }
            else
            {
                _logger.LogWarning(
                    "启动 HTTP 端点预热返回非预期结果。BaseAddress={BaseAddress}, EndpointName={EndpointName}, WarmupMode={WarmupMode}, RelativeUrl={RelativeUrl}, StatusCode={StatusCode}, ApiSuccess={ApiSuccess}, ElapsedMilliseconds={ElapsedMilliseconds}, Message={Message}, ResponseBody={ResponseBody}",
                    _httpClient.BaseAddress,
                    endpointName,
                    warmupMode,
                    relativeUrl,
                    (int)response.StatusCode,
                    apiSuccess,
                    stopwatch.ElapsedMilliseconds,
                    apiMessage ?? string.Empty,
                    Truncate(responseBody));
            }

            return parsedBody;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                ex,
                "启动 HTTP 端点预热失败，已降级跳过。BaseAddress={BaseAddress}, EndpointName={EndpointName}, WarmupMode={WarmupMode}, RelativeUrl={RelativeUrl}, ElapsedMilliseconds={ElapsedMilliseconds}",
                _httpClient.BaseAddress,
                endpointName,
                warmupMode,
                relativeUrl,
                stopwatch.ElapsedMilliseconds);
            return null;
        }
    }

    private static async Task<string> ReadResponseBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var bodyBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        if (IsTextualMediaType(mediaType))
        {
            return Encoding.UTF8.GetString(bodyBytes);
        }

        return $"<binary-response length={bodyBytes.Length} mediaType={mediaType}>";
    }

    private static bool IsTextualMediaType(string mediaType)
    {
        if (string.IsNullOrWhiteSpace(mediaType))
        {
            return true;
        }

        if (mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (mediaType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return mediaType.Contains("xml", StringComparison.OrdinalIgnoreCase);
    }

    private static Uri ResolveBaseAddress(string rawUrl)
    {
        var candidates = rawUrl
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var candidate in candidates)
        {
            if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
            {
                continue;
            }

            var builder = new UriBuilder(uri);
            if (string.Equals(builder.Host, "*", StringComparison.Ordinal)
                || string.Equals(builder.Host, "+", StringComparison.Ordinal)
                || string.Equals(builder.Host, "0.0.0.0", StringComparison.OrdinalIgnoreCase)
                || string.Equals(builder.Host, "::", StringComparison.OrdinalIgnoreCase)
                || string.Equals(builder.Host, "[::]", StringComparison.OrdinalIgnoreCase))
            {
                builder.Host = "127.0.0.1";
            }

            if (string.IsNullOrWhiteSpace(builder.Path))
            {
                builder.Path = "/";
            }

            return builder.Uri;
        }

        return new Uri("http://127.0.0.1:5188/");
    }

    private static JsonDocument? TryParseJson(string responseBody, string mediaType)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        if (!ShouldParseJson(responseBody, mediaType))
        {
            return null;
        }

        try
        {
            return JsonDocument.Parse(responseBody);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// 判断响应体是否应该按 JSON 解析。
    /// </summary>
    private static bool ShouldParseJson(string responseBody, string mediaType)
    {
        if (mediaType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var trimmedBody = responseBody.TrimStart();
        return trimmedBody.StartsWith('{') || trimmedBody.StartsWith('[');
    }

    private static DateTime TruncateToSecond(DateTime value)
    {
        return new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Kind);
    }

    private static bool? TryReadIsSuccess(JsonDocument? document)
    {
        if (document is null)
        {
            return null;
        }

        if (!TryGetProperty(document.RootElement, "isSuccess", out var propertyValue))
        {
            return null;
        }

        return propertyValue.ValueKind == JsonValueKind.True
            ? true
            : propertyValue.ValueKind == JsonValueKind.False
                ? false
                : null;
    }

    private static string? TryReadString(JsonDocument? document, params object[] path)
    {
        if (document is null)
        {
            return null;
        }

        JsonElement current = document.RootElement;
        foreach (var segment in path)
        {
            if (segment is string propertyName)
            {
                if (!TryGetProperty(current, propertyName, out current))
                {
                    return null;
                }

                continue;
            }

            if (segment is int index)
            {
                if (current.ValueKind != JsonValueKind.Array || current.GetArrayLength() <= index)
                {
                    return null;
                }

                current = current[index];
                continue;
            }

            return null;
        }

        return current.ValueKind == JsonValueKind.String
            ? current.GetString()
            : current.ToString();
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string Truncate(string responseBody)
    {
        return responseBody.Length <= 1024
            ? responseBody
            : $"{responseBody[..1024]}...(已截断，原始长度={responseBody.Length})";
    }

    private sealed record WarmupDiscoveryContext(
        string WaveCode,
        string TaskCode,
        string Barcode,
        string? OrderId);
}
