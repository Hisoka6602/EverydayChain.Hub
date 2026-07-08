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
/// 提供总看板查询、导出与手工同步接口，用于展示整体分拣进度、读码表现与数据同步状态。
/// </summary>
[ApiController]
[Route("api/v1/dashboard")]
public sealed class GlobalDashboardController : QueryControllerBase
{
    private static readonly UTF8Encoding Utf8EncodingWithBom = new(true);

    /// <summary>
    /// 存储 _globalDashboardQueryService 字段。
    /// </summary>
    private readonly IGlobalDashboardQueryService _globalDashboardQueryService;
    /// <summary>
    /// 存储 _syncOrchestrator 字段。
    /// </summary>
    private readonly ISyncOrchestrator _syncOrchestrator;

    /// <summary>
    /// 执行 GlobalDashboardController 方法。
    /// </summary>
    public GlobalDashboardController(
        IGlobalDashboardQueryService globalDashboardQueryService,
        ISyncOrchestrator? syncOrchestrator = null)
    {
        // 步骤：执行 GlobalDashboardController 方法的核心处理流程。
        _globalDashboardQueryService = globalDashboardQueryService;
        _syncOrchestrator = syncOrchestrator ?? new NoopSyncOrchestrator();
    }

    /// <summary>
    /// 查询总看板汇总，返回指定时间段内的总件量、待分拣量、分拣进度、回流量、异常量与同步进度。
    /// </summary>
    /// <param name="request">请求体查询条件，指定统计时间范围。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>总看板汇总结果。</returns>
    [HttpPost("overview")]
    [ProducesResponseType(typeof(ApiResponse<GlobalDashboardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GlobalDashboardResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<GlobalDashboardResponse>>> QueryOverviewAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] GlobalDashboardQueryRequest? request,
        [FromQuery] GlobalDashboardQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 QueryOverviewAsync 方法的核心处理流程。
        if (!TryResolveQuery(request, queryRequest, out var resolvedRequest, out var validationResult))
        {
            return validationResult!;
        }

        var result = await _globalDashboardQueryService.QueryAsync(resolvedRequest!, cancellationToken);
        return Ok(ApiResponse<GlobalDashboardResponse>.Success(BuildResponse(result), "Dashboard overview query succeeded."));
    }

    /// <summary>
    /// 导出总看板 CSV 文件，包含页面展示的核心统计指标与汇总数据。
    /// </summary>
    /// <param name="request">请求体查询条件。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>CSV 文件流。</returns>
    [HttpPost("export/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportOverviewCsvAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] GlobalDashboardQueryRequest? request,
        [FromQuery] GlobalDashboardQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 ExportOverviewCsvAsync 方法的核心处理流程。
        if (!TryResolveQuery(request, queryRequest, out var resolvedRequest, out var validationResult))
        {
            return validationResult!;
        }

        var result = await _globalDashboardQueryService.QueryAsync(resolvedRequest!, cancellationToken);
        var fileName = $"dashboard-overview-{DateTimeOffset.Now.LocalDateTime:yyyyMMddHHmmss}.csv";
        return File(BuildUtf8BomCsvBytes(BuildCsv(result)), "text/csv; charset=utf-8", fileName);
    }

    /// <summary>
    /// 导出总看板 Excel 文件，便于业务侧做二次整理与存档。
    /// </summary>
    /// <param name="request">请求体查询条件。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Excel 文件流。</returns>
    [HttpPost("export/xlsx")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportOverviewXlsxAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] GlobalDashboardQueryRequest? request,
        [FromQuery] GlobalDashboardQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 ExportOverviewXlsxAsync 方法的核心处理流程。
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
    /// 手工触发同步任务，可指定单张源表或对全部启用表执行一次立即同步。
    /// </summary>
    /// <param name="request">手工同步请求，可选指定表编码。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>同步批次执行结果。</returns>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(ApiResponse<ManualSyncResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ManualSyncResponse>>> TriggerSyncAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ManualSyncRequest? request,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 TriggerSyncAsync 方法的核心处理流程。
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
    /// 执行 TryResolveQuery 方法。
    /// </summary>
    private bool TryResolveQuery(
        GlobalDashboardQueryRequest? request,
        GlobalDashboardQueryRequest? queryRequest,
        out EverydayChain.Hub.Application.Models.GlobalDashboardQueryRequest? resolvedRequest,
        out ActionResult? validationResult)
    {
        // 步骤：执行 TryResolveQuery 方法的核心处理流程。
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
    /// 定义 NoopSyncOrchestrator 类型。
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

