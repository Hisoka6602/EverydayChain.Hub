using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var options = CommandLineOptions.Parse(args);
var jsonOptions = CreateJsonOptions();
using var httpClient = new HttpClient
{
    BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute),
    Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
};

await WaitForWarmupCompletedAsync(httpClient, options.WarmupWaitSeconds);

var now = DateTime.Now;
var startTimeLocal = now.AddHours(-24);
var endTimeLocal = now;

var discoveryCurrent = await SendJsonAsync(httpClient, HttpMethod.Post, "/api/v1/waves/current", new
{
    startTimeLocal = FormatLocalTime(startTimeLocal),
    endTimeLocal = FormatLocalTime(endTimeLocal)
}, jsonOptions);
var discoveryOptions = await SendJsonAsync(httpClient, HttpMethod.Post, "/api/v1/waves/options", new
{
    startTimeLocal = FormatLocalTime(startTimeLocal),
    endTimeLocal = FormatLocalTime(endTimeLocal)
}, jsonOptions);
var discoveryTasks = await SendJsonAsync(httpClient, HttpMethod.Post, "/api/v1/business-query/tasks", new
{
    startTimeLocal = FormatLocalTime(startTimeLocal),
    endTimeLocal = FormatLocalTime(endTimeLocal),
    pageNumber = 1,
    pageSize = 100
}, jsonOptions);

var waveCode =
    TryReadString(discoveryCurrent.JsonDocument, "data", "waveCode")
    ?? TryReadString(discoveryOptions.JsonDocument, "data", "waveOptions", "0", "waveCode")
    ?? TryReadString(discoveryTasks.JsonDocument, "data", "items", "0", "waveCode")
    ?? "WARMUP";
var taskCode =
    TryReadString(discoveryTasks.JsonDocument, "data", "items", "0", "taskCode")
    ?? "WARMUP-TASK";
var barcode =
    TryReadString(discoveryTasks.JsonDocument, "data", "items", "0", "barcode")
    ?? TryReadString(discoveryCurrent.JsonDocument, "data", "barcode")
    ?? "WARMUP-BARCODE";
var orderId =
    TryReadString(discoveryTasks.JsonDocument, "data", "items", "0", "orderId");

var cases = BuildCases(FormatLocalTime(startTimeLocal), FormatLocalTime(endTimeLocal), waveCode, taskCode, barcode, orderId, jsonOptions);
var results = new List<EndpointResult>(cases.Count);

foreach (var endpointCase in cases)
{
    Console.WriteLine($"Running {endpointCase.Name} ({endpointCase.Method} {endpointCase.Path})...");

    var first = await SendCaseAsync(httpClient, endpointCase);
    var second = await SendCaseAsync(httpClient, endpointCase);

    results.Add(new EndpointResult(
        endpointCase.Name,
        endpointCase.Method.Method,
        endpointCase.Path,
        string.Join(",", endpointCase.ExpectedStatusCodes.OrderBy(code => code)),
        first.StatusCode,
        first.ElapsedMs,
        second.StatusCode,
        second.ElapsedMs,
        endpointCase.ExpectedStatusCodes.Contains(first.StatusCode),
        endpointCase.ExpectedStatusCodes.Contains(second.StatusCode),
        first.ContentType ?? string.Empty,
        first.Preview ?? string.Empty));
}

var summary = new BenchmarkSummary(options.BaseUrl, waveCode, taskCode, barcode, orderId ?? string.Empty, results);
var outputJson = JsonSerializer.Serialize(summary, jsonOptions);
if (!string.IsNullOrWhiteSpace(options.OutputJson))
{
    var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(options.OutputJson));
    if (!string.IsNullOrWhiteSpace(outputDirectory))
    {
        Directory.CreateDirectory(outputDirectory);
    }

    await File.WriteAllTextAsync(options.OutputJson, outputJson, Encoding.UTF8);
}

Console.WriteLine();
Console.WriteLine($"Completed {results.Count} endpoints for {options.BaseUrl}");
foreach (var result in results)
{
    Console.WriteLine(
        $"{result.Name,-32} {result.FirstStatusCode,3}/{result.SecondStatusCode,3}  first={result.FirstElapsedMs,8:F2} ms  second={result.SecondElapsedMs,8:F2} ms  path={result.Path}");
}

static async Task WaitForWarmupCompletedAsync(HttpClient httpClient, int warmupWaitSeconds)
{
    var deadline = DateTime.Now.AddSeconds(warmupWaitSeconds);

    while (DateTime.Now <= deadline)
    {
        var result = await SendAsync(httpClient, HttpMethod.Get, "/health/ready", null, "application/json");
        if (result.JsonDocument is not null
            && TryReadBoolean(result.JsonDocument, "data", "apiWarmup", "isCompleted") == true)
        {
            return;
        }

        await Task.Delay(TimeSpan.FromSeconds(1));
    }

    throw new TimeoutException($"API warmup did not complete within {warmupWaitSeconds} seconds.");
}

static List<EndpointCase> BuildCases(
    string startTimeLocal,
    string endTimeLocal,
    string waveCode,
    string taskCode,
    string barcode,
    string? orderId,
    JsonSerializerOptions jsonOptions)
{
    return
    [
        new("Root", HttpMethod.Get, "/", null, "application/json", [200]),
        new("HealthLive", HttpMethod.Get, "/health/live", null, "application/json", [200]),
        new("HealthReady", HttpMethod.Get, "/health/ready", null, "application/json", [200, 503]),
        new("BusinessTaskSeedManual", HttpMethod.Post, "/api/v1/business-task-seed/manual", JsonSerializer.SerializeToUtf8Bytes(new
        {
            targetTableName = "business_tasks",
            barcodes = Array.Empty<string>()
        }, jsonOptions), "application/json", [400]),
        new("BusinessQueryTasks", HttpMethod.Post, "/api/v1/business-query/tasks", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal,
            pageNumber = 1,
            pageSize = 100
        }, jsonOptions), "application/json", [200]),
        new("BusinessQueryExceptions", HttpMethod.Post, "/api/v1/business-query/exceptions", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal,
            pageNumber = 1,
            pageSize = 100
        }, jsonOptions), "application/json", [200]),
        new("BusinessQueryRecirculations", HttpMethod.Post, "/api/v1/business-query/recirculations", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal,
            pageNumber = 1,
            pageSize = 100
        }, jsonOptions), "application/json", [200]),
        new("BoxTrackingQuery", HttpMethod.Post, "/api/v1/box-tracking/query", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal,
            boxId = barcode,
            orderId,
            pageNumber = 1,
            pageSize = 100
        }, jsonOptions), "application/json", [200]),
        new("BoxTrackingExportCsv", HttpMethod.Post, "/api/v1/box-tracking/export/csv", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal,
            boxId = barcode,
            orderId
        }, jsonOptions), "application/json", [200]),
        new("BoxTrackingExportXlsx", HttpMethod.Post, "/api/v1/box-tracking/export/xlsx", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal,
            boxId = barcode,
            orderId
        }, jsonOptions), "application/json", [200]),
        new("ExportsCatalog", HttpMethod.Post, "/api/v1/exports/catalog", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal
        }, jsonOptions), "application/json", [200]),
        new("DropFeedbackConfirm", HttpMethod.Post, "/api/v1/drop-feedback/confirm", JsonSerializer.SerializeToUtf8Bytes(new
        {
            taskCode = "WARMUP-TASK-NOT-FOUND",
            barcode = "WARMUP-BARCODE-NOT-FOUND",
            actualChuteCode = "WARMUP-CHUTE",
            dropTimeLocal = endTimeLocal,
            isSuccess = true
        }, jsonOptions), "application/json", [200]),
        new("DockDashboardOverview", HttpMethod.Post, "/api/v1/dock-dashboard/overview", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal,
            waveCode
        }, jsonOptions), "application/json", [200]),
        new("DockDashboardExportCsv", HttpMethod.Post, "/api/v1/dock-dashboard/export/csv", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal,
            waveCode
        }, jsonOptions), "application/json", [200]),
        new("DockDashboardExportXlsx", HttpMethod.Post, "/api/v1/dock-dashboard/export/xlsx", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal,
            waveCode
        }, jsonOptions), "application/json", [200]),
        new("DashboardOverview", HttpMethod.Post, "/api/v1/dashboard/overview", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal
        }, jsonOptions), "application/json", [200]),
        new("DashboardExportCsv", HttpMethod.Post, "/api/v1/dashboard/export/csv", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal
        }, jsonOptions), "application/json", [200]),
        new("DashboardExportXlsx", HttpMethod.Post, "/api/v1/dashboard/export/xlsx", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal
        }, jsonOptions), "application/json", [200]),
        new("DashboardSync", HttpMethod.Post, "/api/v1/dashboard/sync", Encoding.UTF8.GetBytes("{"), "application/json", [400]),
        new("ChuteResolve", HttpMethod.Post, "/api/v1/chute/resolve", JsonSerializer.SerializeToUtf8Bytes(new
        {
            taskCode,
            barcode
        }, jsonOptions), "application/json", [200]),
        new("RecirculationsSummary", HttpMethod.Post, "/api/v1/recirculations/summary", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal,
            sortOrder = "Most"
        }, jsonOptions), "application/json", [200]),
        new("RecirculationsDetails", HttpMethod.Post, "/api/v1/recirculations/details", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal,
            pageNumber = 1,
            pageSize = 100
        }, jsonOptions), "application/json", [200]),
        new("RecirculationsSummaryExportCsv", HttpMethod.Post, "/api/v1/recirculations/summary/export/csv", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal,
            sortOrder = "Most"
        }, jsonOptions), "application/json", [200]),
        new("RecirculationsSummaryExportXlsx", HttpMethod.Post, "/api/v1/recirculations/summary/export/xlsx", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal,
            sortOrder = "Most"
        }, jsonOptions), "application/json", [200]),
        new("RetentionCleanupsQuery", HttpMethod.Post, "/api/v1/retention-cleanups/query", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal,
            pageNumber = 1,
            pageSize = 100
        }, jsonOptions), "application/json", [200]),
        new("ScanUpload", HttpMethod.Post, "/api/v1/scan/upload", JsonSerializer.SerializeToUtf8Bytes(new
        {
            barcodes = Array.Empty<string>(),
            deviceCode = "WARMUP-DEVICE",
            scanTimeLocal = endTimeLocal,
            traceId = "WARMUP-TRACE"
        }, jsonOptions), "application/json", [400]),
        new("ReportsQuery", HttpMethod.Post, "/api/v1/reports/query", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal
        }, jsonOptions), "application/json", [200]),
        new("ReportsExportCsv", HttpMethod.Post, "/api/v1/reports/export/csv", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal
        }, jsonOptions), "application/json", [200]),
        new("ReportsExportXlsx", HttpMethod.Post, "/api/v1/reports/export/xlsx", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal
        }, jsonOptions), "application/json", [200]),
        new("WavesCurrent", HttpMethod.Post, "/api/v1/waves/current", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal
        }, jsonOptions), "application/json", [200]),
        new("WavesOptions", HttpMethod.Post, "/api/v1/waves/options", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal
        }, jsonOptions), "application/json", [200]),
        new("WavesSummary", HttpMethod.Post, "/api/v1/waves/summary", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal,
            waveCode
        }, jsonOptions), "application/json", [200, 400]),
        new("WavesZones", HttpMethod.Post, "/api/v1/waves/zones", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal,
            waveCode
        }, jsonOptions), "application/json", [200, 400]),
        new("WavesZonesExportCsv", HttpMethod.Post, "/api/v1/waves/zones/export/csv", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal,
            waveCode
        }, jsonOptions), "application/json", [200, 400]),
        new("WavesZonesExportXlsx", HttpMethod.Post, "/api/v1/waves/zones/export/xlsx", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal,
            waveCode
        }, jsonOptions), "application/json", [200, 400]),
        new("WavesList", HttpMethod.Post, "/api/v1/waves/list", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal
        }, jsonOptions), "application/json", [200]),
        new("WavesListExportCsv", HttpMethod.Post, "/api/v1/waves/list/export/csv", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal
        }, jsonOptions), "application/json", [200]),
        new("WavesListExportXlsx", HttpMethod.Post, "/api/v1/waves/list/export/xlsx", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal
        }, jsonOptions), "application/json", [200]),
        new("WavesDetails", HttpMethod.Post, "/api/v1/waves/details", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal,
            waveCode
        }, jsonOptions), "application/json", [200]),
        new("WavesDetailsExportCsv", HttpMethod.Post, "/api/v1/waves/details/export/csv", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal,
            waveCode
        }, jsonOptions), "application/json", [200]),
        new("WavesDetailsExportXlsx", HttpMethod.Post, "/api/v1/waves/details/export/xlsx", JsonSerializer.SerializeToUtf8Bytes(new
        {
            startTimeLocal,
            endTimeLocal,
            waveCode
        }, jsonOptions), "application/json", [200]),
        new("WaveCleanupQuery", HttpMethod.Post, "/api/v1/wave-cleanup/query", JsonSerializer.SerializeToUtf8Bytes(new
        {
            waveCode
        }, jsonOptions), "application/json", [200]),
        new("WaveCleanupDryRun", HttpMethod.Post, "/api/v1/wave-cleanup/dry-run", JsonSerializer.SerializeToUtf8Bytes(new
        {
            waveCode
        }, jsonOptions), "application/json", [200]),
        new("WaveCleanupExecute", HttpMethod.Post, "/api/v1/wave-cleanup/execute", JsonSerializer.SerializeToUtf8Bytes(new
        {
            waveCode = string.Empty
        }, jsonOptions), "application/json", [400])
    ];
}

static async Task<HttpResult> SendCaseAsync(HttpClient httpClient, EndpointCase endpointCase)
{
    return await SendAsync(httpClient, endpointCase.Method, endpointCase.Path, endpointCase.Body, endpointCase.ContentType);
}

static async Task<HttpResult> SendJsonAsync(HttpClient httpClient, HttpMethod method, string path, object payload, JsonSerializerOptions jsonOptions)
{
    return await SendAsync(
        httpClient,
        method,
        path,
        JsonSerializer.SerializeToUtf8Bytes(payload, jsonOptions),
        "application/json");
}

static async Task<HttpResult> SendAsync(
    HttpClient httpClient,
    HttpMethod method,
    string path,
    byte[]? body,
    string contentType)
{
    using var request = new HttpRequestMessage(method, path);
    if (body is not null)
    {
        request.Content = new ByteArrayContent(body);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
    }

    var stopwatch = Stopwatch.StartNew();
    using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
    byte[] responseBytes = await response.Content.ReadAsByteArrayAsync();
    stopwatch.Stop();

    string? responseContentType = response.Content.Headers.ContentType?.MediaType;
    string preview = BuildPreview(responseBytes, responseContentType);
    JsonDocument? jsonDocument = TryParseJson(responseBytes, responseContentType);

    return new HttpResult(
        (int)response.StatusCode,
        RoundMilliseconds(stopwatch.ElapsedTicks),
        responseContentType,
        preview,
        jsonDocument);
}

static decimal RoundMilliseconds(long elapsedTicks)
{
    // 存储每毫秒对应的 Tick 数，用于将 Stopwatch Tick 转换为毫秒。
    const decimal ticksPerMillisecond = TimeSpan.TicksPerMillisecond;
    var milliseconds = elapsedTicks / ticksPerMillisecond;
    return decimal.Round(milliseconds, 3, MidpointRounding.AwayFromZero);
}

static string BuildPreview(byte[] responseBytes, string? contentType)
{
    if (responseBytes.Length == 0)
    {
        return string.Empty;
    }

    if (contentType is not null
        && (contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("text", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("xml", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("csv", StringComparison.OrdinalIgnoreCase)))
    {
        var text = Encoding.UTF8.GetString(responseBytes);
        return text.Length <= 512 ? text : text[..512];
    }

    return $"[{contentType ?? "binary"} {responseBytes.Length} bytes]";
}

static JsonDocument? TryParseJson(byte[] responseBytes, string? contentType)
{
    if (responseBytes.Length == 0)
    {
        return null;
    }

    if (contentType is null || !contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
    {
        return null;
    }

    try
    {
        return JsonDocument.Parse(responseBytes);
    }
    catch
    {
        return null;
    }
}

static string? TryReadString(JsonDocument? document, params string[] path)
{
    var element = TryReadElement(document, path);
    return element is { ValueKind: JsonValueKind.String } ? element.Value.GetString() : null;
}

static bool? TryReadBoolean(JsonDocument? document, params string[] path)
{
    var element = TryReadElement(document, path);
    return element is { ValueKind: JsonValueKind.True } ? true
        : element is { ValueKind: JsonValueKind.False } ? false
        : null;
}

static JsonElement? TryReadElement(JsonDocument? document, params string[] path)
{
    if (document is null)
    {
        return null;
    }

    JsonElement current = document.RootElement;
    foreach (var segment in path)
    {
        if (current.ValueKind == JsonValueKind.Object)
        {
            if (!current.TryGetProperty(segment, out current))
            {
                return null;
            }

            continue;
        }

        if (current.ValueKind == JsonValueKind.Array && int.TryParse(segment, out var index))
        {
            if (index < 0 || index >= current.GetArrayLength())
            {
                return null;
            }

            current = current[index];
            continue;
        }

        return null;
    }

    return current;
}

static string FormatLocalTime(DateTime value)
{
    return value.ToString("yyyy-MM-dd HH:mm:ss");
}

static JsonSerializerOptions CreateJsonOptions()
{
    return new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
}

sealed record CommandLineOptions(string BaseUrl, int TimeoutSeconds, int WarmupWaitSeconds, string OutputJson)
{
    public static CommandLineOptions Parse(string[] args)
    {
        string? baseUrl = null;
        var timeoutSeconds = 60;
        var warmupWaitSeconds = 240;
        var outputJson = string.Empty;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--baseUrl":
                    baseUrl = GetValue(args, ref index);
                    break;
                case "--timeoutSeconds":
                    timeoutSeconds = int.Parse(GetValue(args, ref index));
                    break;
                case "--warmupWaitSeconds":
                    warmupWaitSeconds = int.Parse(GetValue(args, ref index));
                    break;
                case "--outputJson":
                    outputJson = GetValue(args, ref index);
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("Missing required argument --baseUrl");
        }

        return new CommandLineOptions(baseUrl.TrimEnd('/'), timeoutSeconds, warmupWaitSeconds, outputJson);
    }

    private static string GetValue(string[] args, ref int index)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for argument {args[index]}");
        }

        index++;
        return args[index];
    }
}

sealed record EndpointCase(
    string Name,
    HttpMethod Method,
    string Path,
    byte[]? Body,
    string ContentType,
    HashSet<int> ExpectedStatusCodes);

sealed record HttpResult(
    int StatusCode,
    decimal ElapsedMs,
    string? ContentType,
    string Preview,
    JsonDocument? JsonDocument);

sealed record EndpointResult(
    string Name,
    string Method,
    string Path,
    string ExpectedStatusCodes,
    int FirstStatusCode,
    decimal FirstElapsedMs,
    int SecondStatusCode,
    decimal SecondElapsedMs,
    bool FirstStatusMatched,
    bool SecondStatusMatched,
    string ContentType,
    string Preview);

sealed record BenchmarkSummary(
    string BaseUrl,
    string WaveCode,
    string TaskCode,
    string Barcode,
    string OrderId,
    IReadOnlyList<EndpointResult> Results);
