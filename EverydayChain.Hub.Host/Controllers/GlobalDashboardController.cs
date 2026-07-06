using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 定义当前类型。
/// </summary>
[ApiController]
[Route("api/v1/dashboard")]
public sealed class GlobalDashboardController : QueryControllerBase
{
    private static readonly UTF8Encoding Utf8EncodingWithBom = new(true);

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly IGlobalDashboardQueryService _globalDashboardQueryService;
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly ISyncOrchestrator _syncOrchestrator;

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public GlobalDashboardController(
        IGlobalDashboardQueryService globalDashboardQueryService,
        ISyncOrchestrator? syncOrchestrator = null)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        _globalDashboardQueryService = globalDashboardQueryService;
        _syncOrchestrator = syncOrchestrator ?? new NoopSyncOrchestrator();
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [HttpPost("overview")]
    [ProducesResponseType(typeof(ApiResponse<GlobalDashboardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GlobalDashboardResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<GlobalDashboardResponse>>> QueryOverviewAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] GlobalDashboardQueryRequest? request,
        [FromQuery] GlobalDashboardQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        if (!TryResolveQuery(request, queryRequest, out var resolvedRequest, out var validationResult))
        {
            return validationResult!;
        }

        var result = await _globalDashboardQueryService.QueryAsync(resolvedRequest!, cancellationToken);
        return Ok(ApiResponse<GlobalDashboardResponse>.Success(BuildResponse(result), "Dashboard overview query succeeded."));
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [HttpPost("export/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportOverviewCsvAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] GlobalDashboardQueryRequest? request,
        [FromQuery] GlobalDashboardQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        if (!TryResolveQuery(request, queryRequest, out var resolvedRequest, out var validationResult))
        {
            return validationResult!;
        }

        var result = await _globalDashboardQueryService.QueryAsync(resolvedRequest!, cancellationToken);
        var fileName = $"dashboard-overview-{DateTimeOffset.Now.LocalDateTime:yyyyMMddHHmmss}.csv";
        return File(BuildUtf8BomCsvBytes(BuildCsv(result)), "text/csv; charset=utf-8", fileName);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [HttpPost("export/xlsx")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportOverviewXlsxAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] GlobalDashboardQueryRequest? request,
        [FromQuery] GlobalDashboardQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        if (!TryResolveQuery(request, queryRequest, out var resolvedRequest, out var validationResult))
        {
            return validationResult!;
        }

        var result = await _globalDashboardQueryService.QueryAsync(resolvedRequest!, cancellationToken);
        var rows = BuildTabularRows(result);
        var content = SimpleXlsxBuilder.BuildSingleSheet("DashboardOverview", ["Metric", "Value"], rows);
        var fileName = $"dashboard-overview-{DateTimeOffset.Now.LocalDateTime:yyyyMMddHHmmss}.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(ApiResponse<ManualSyncResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ManualSyncResponse>>> TriggerSyncAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ManualSyncRequest? request,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        var triggeredAtLocal = DateTime.Now;
        var normalizedTableCode = string.IsNullOrWhiteSpace(request?.TableCode) ? null : request.TableCode.Trim();
        IReadOnlyList<SyncBatchResult> results = normalizedTableCode is null
            ? await _syncOrchestrator.RunAllEnabledTableSyncAsync(cancellationToken)
            : [await _syncOrchestrator.RunTableSyncAsync(normalizedTableCode, cancellationToken)];

        var response = new ManualSyncResponse
        {
            TriggeredAtLocal = triggeredAtLocal,
            TotalBatchCount = results.Count,
            SuccessBatchCount = results.Count(item => string.IsNullOrWhiteSpace(item.FailureMessage)),
            FailedBatchCount = results.Count(item => !string.IsNullOrWhiteSpace(item.FailureMessage)),
            Items = results
                .Select(item => new ManualSyncBatchResponse
                {
                    BatchId = item.BatchId,
                    TableCode = item.TableCode,
                    Status = string.IsNullOrWhiteSpace(item.FailureMessage) ? "Completed" : "Failed",
                    ReadCount = item.ReadCount,
                    InsertCount = item.InsertCount,
                    UpdateCount = item.UpdateCount,
                    DeleteCount = item.DeleteCount,
                    SkipCount = item.SkipCount,
                    ErrorMessage = item.FailureMessage
                })
                .ToList()
        };
        return Ok(ApiResponse<ManualSyncResponse>.Success(response, "Manual sync completed."));
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private bool TryResolveQuery(
        GlobalDashboardQueryRequest? request,
        GlobalDashboardQueryRequest? queryRequest,
        out EverydayChain.Hub.Application.Models.GlobalDashboardQueryRequest? resolvedRequest,
        out ActionResult? validationResult)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        resolvedRequest = null;
        validationResult = null;

        var resolved = ResolveRequest(request, queryRequest);
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(
                resolved.StartTimeLocal,
                resolved.EndTimeLocal,
                out var normalizedStartTime,
                out var normalizedEndTime,
                out var validationMessage))
        {
            validationResult = BadRequest(ApiResponse<object>.Fail(validationMessage));
            return false;
        }

        resolvedRequest = new EverydayChain.Hub.Application.Models.GlobalDashboardQueryRequest
        {
            StartTimeLocal = normalizedStartTime,
            EndTimeLocal = normalizedEndTime
        };
        return true;
    }

    private static GlobalDashboardResponse BuildResponse(EverydayChain.Hub.Application.Models.GlobalDashboardQueryResult result)
    {
        return new GlobalDashboardResponse
        {
            TotalCount = result.TotalCount,
            UnsortedCount = result.UnsortedCount,
            TotalSortedProgressPercent = result.TotalSortedProgressPercent,
            FullCaseTotalCount = result.FullCaseTotalCount,
            FullCaseUnsortedCount = result.FullCaseUnsortedCount,
            FullCaseSortedProgressPercent = result.FullCaseSortedProgressPercent,
            SplitTotalCount = result.SplitTotalCount,
            SplitUnsortedCount = result.SplitUnsortedCount,
            SplitSortedProgressPercent = result.SplitSortedProgressPercent,
            RecognitionRatePercent = result.RecognitionRatePercent,
            RecirculatedCount = result.RecirculatedCount,
            ExceptionCount = result.ExceptionCount,
            TotalVolumeMm3 = result.TotalVolumeMm3,
            TotalWeightGram = result.TotalWeightGram,
            LatestSyncTimeLocal = result.LatestSyncTimeLocal,
            DataDownloadProgressPercent = result.DataDownloadProgressPercent,
            DataWritebackProgressPercent = result.DataWritebackProgressPercent,
            WaveSummaries = result.WaveSummaries
                .Select(summary => new WaveDashboardSummaryResponse
                {
                    WaveCode = summary.WaveCode,
                    TotalCount = summary.TotalCount,
                    UnsortedCount = summary.UnsortedCount,
                    SortedProgressPercent = summary.SortedProgressPercent
                })
                .ToList()
        };
    }

    private static string BuildCsv(EverydayChain.Hub.Application.Models.GlobalDashboardQueryResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Metric,Value");
        foreach (var row in BuildTabularRows(result))
        {
            builder.AppendLine($"{EscapeCsvField(row[0])},{EscapeCsvField(row[1])}");
        }

        return builder.ToString();
    }

    private static IReadOnlyList<IReadOnlyList<string?>> BuildTabularRows(EverydayChain.Hub.Application.Models.GlobalDashboardQueryResult result)
    {
        return
        [
            ["TotalCount", result.TotalCount.ToString()],
            ["UnsortedCount", result.UnsortedCount.ToString()],
            ["TotalSortedProgressPercent", result.TotalSortedProgressPercent.ToString("0.##")],
            ["FullCaseTotalCount", result.FullCaseTotalCount.ToString()],
            ["FullCaseUnsortedCount", result.FullCaseUnsortedCount.ToString()],
            ["FullCaseSortedProgressPercent", result.FullCaseSortedProgressPercent.ToString("0.##")],
            ["SplitTotalCount", result.SplitTotalCount.ToString()],
            ["SplitUnsortedCount", result.SplitUnsortedCount.ToString()],
            ["SplitSortedProgressPercent", result.SplitSortedProgressPercent.ToString("0.##")],
            ["RecognitionRatePercent", result.RecognitionRatePercent.ToString("0.##")],
            ["RecirculatedCount", result.RecirculatedCount.ToString()],
            ["ExceptionCount", result.ExceptionCount.ToString()],
            ["TotalVolumeMm3", result.TotalVolumeMm3.ToString("0.##")],
            ["TotalWeightGram", result.TotalWeightGram.ToString("0.##")],
            ["LatestSyncTimeLocal", result.LatestSyncTimeLocal?.ToString("yyyy-MM-dd HH:mm:ss")],
            ["DataDownloadProgressPercent", result.DataDownloadProgressPercent.ToString("0.##")],
            ["DataWritebackProgressPercent", result.DataWritebackProgressPercent.ToString("0.##")]
        ];
    }

    private static byte[] BuildUtf8BomCsvBytes(string csvContent)
    {
        var preamble = Utf8EncodingWithBom.GetPreamble();
        var contentBytes = Utf8EncodingWithBom.GetBytes(csvContent);
        var bytes = new byte[preamble.Length + contentBytes.Length];
        Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
        Buffer.BlockCopy(contentBytes, 0, bytes, preamble.Length, contentBytes.Length);
        return bytes;
    }

    private static string EscapeCsvField(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    /// <summary>
    /// 定义当前类型。
    /// </summary>
    private sealed class NoopSyncOrchestrator : ISyncOrchestrator
    {
        public Task<SyncBatchResult> RunTableSyncAsync(string tableCode, CancellationToken ct)
        {
            return Task.FromResult(new SyncBatchResult
            {
                BatchId = string.Empty,
                TableCode = tableCode,
                FailureMessage = "Sync orchestrator was not configured."
            });
        }

        public Task<IReadOnlyList<SyncBatchResult>> RunAllEnabledTableSyncAsync(CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<SyncBatchResult>>([]);
        }
    }
}

