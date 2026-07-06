using EverydayChain.Hub.Application.Abstractions.Queries;
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
[Route("api/v1/dock-dashboard")]
public sealed class DockDashboardController : QueryControllerBase
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private static readonly UTF8Encoding Utf8EncodingWithBom = new(true);

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly IDockDashboardQueryService _dockDashboardQueryService;

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public DockDashboardController(IDockDashboardQueryService dockDashboardQueryService)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        _dockDashboardQueryService = dockDashboardQueryService;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [HttpPost("overview")]
    [ProducesResponseType(typeof(ApiResponse<DockDashboardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DockDashboardResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<DockDashboardResponse>>> QueryOverviewAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DockDashboardQueryRequest? request,
        [FromQuery] DockDashboardQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        var resolvedRequest = ResolveRequest(request, queryRequest);
        var todayLocal = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Local);
        if (!LocalTimeRangeValidator.TryNormalizeOptionalRange(resolvedRequest.StartTimeLocal, resolvedRequest.EndTimeLocal, todayLocal, out var normalizedStart, out var normalizedEnd, out var validationMessage))
        {
            return BadRequest(ApiResponse<DockDashboardResponse>.Fail(validationMessage));
        }

        var result = await _dockDashboardQueryService.QueryAsync(new EverydayChain.Hub.Application.Models.DockDashboardQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd,
            WaveCode = resolvedRequest.WaveCode
        }, cancellationToken);

        var response = new DockDashboardResponse
        {
            StartTimeLocal = result.StartTimeLocal,
            EndTimeLocal = result.EndTimeLocal,
            SelectedWaveCode = result.SelectedWaveCode,
            WaveOptions = result.WaveOptions,
            DockSummaries = result.DockSummaries
                .Select(summary => new DockDashboardSummaryResponse
                {
                    DockCode = summary.DockCode,
                    SplitUnsortedCount = summary.SplitUnsortedCount,
                    FullCaseUnsortedCount = summary.FullCaseUnsortedCount,
                    RecirculatedCount = summary.RecirculatedCount,
                    ExceptionCount = summary.ExceptionCount,
                    SortedProgressPercent = summary.SortedProgressPercent,
                    SortedCount = summary.SortedCount
                })
                .ToList()
        };

        return Ok(ApiResponse<DockDashboardResponse>.Success(response, "码头看板查询成功。"));
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [HttpPost("export/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportCsvAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DockDashboardQueryRequest? request,
        [FromQuery] DockDashboardQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        var queryResult = await QueryInternalAsync(request, queryRequest, cancellationToken);
        if (queryResult.Result is not null)
        {
            return queryResult.Result;
        }

        var csvContent = BuildCsv(queryResult.Value!);
        var fileName = $"dock-dashboard-{DateTimeOffset.Now.LocalDateTime:yyyyMMddHHmmss}.csv";
        return File(BuildUtf8BomCsvBytes(csvContent), "text/csv; charset=utf-8", fileName);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [HttpPost("export/xlsx")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportXlsxAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DockDashboardQueryRequest? request,
        [FromQuery] DockDashboardQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        var queryResult = await QueryInternalAsync(request, queryRequest, cancellationToken);
        if (queryResult.Result is not null)
        {
            return queryResult.Result;
        }

        var content = SimpleXlsxBuilder.BuildSingleSheet(
            "DockDashboard",
            ["DockCode", "SplitUnsortedCount", "FullCaseUnsortedCount", "RecirculatedCount", "ExceptionCount", "SortedCount", "SortedProgressPercent"],
            queryResult.Value!.DockSummaries
                .Select(summary => (IReadOnlyList<string?>)
                [
                    summary.DockCode,
                    summary.SplitUnsortedCount.ToString(),
                    summary.FullCaseUnsortedCount.ToString(),
                    summary.RecirculatedCount.ToString(),
                    summary.ExceptionCount.ToString(),
                    summary.SortedCount.ToString(),
                    summary.SortedProgressPercent.ToString("0.##")
                ])
                .ToList());
        var fileName = $"dock-dashboard-{DateTimeOffset.Now.LocalDateTime:yyyyMMddHHmmss}.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private async Task<(EverydayChain.Hub.Application.Models.DockDashboardQueryResult? Value, ActionResult? Result)> QueryInternalAsync(
        DockDashboardQueryRequest? request,
        DockDashboardQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        var resolvedRequest = ResolveRequest(request, queryRequest);
        var todayLocal = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Local);
        if (!LocalTimeRangeValidator.TryNormalizeOptionalRange(resolvedRequest.StartTimeLocal, resolvedRequest.EndTimeLocal, todayLocal, out var normalizedStart, out var normalizedEnd, out var validationMessage))
        {
            return (null, BadRequest(ApiResponse<object>.Fail(validationMessage)));
        }

        var result = await _dockDashboardQueryService.QueryAsync(new EverydayChain.Hub.Application.Models.DockDashboardQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd,
            WaveCode = resolvedRequest.WaveCode
        }, cancellationToken);
        return (result, null);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static string BuildCsv(EverydayChain.Hub.Application.Models.DockDashboardQueryResult result)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        var builder = new StringBuilder();
        builder.AppendLine("DockCode,SplitUnsortedCount,FullCaseUnsortedCount,RecirculatedCount,ExceptionCount,SortedCount,SortedProgressPercent");
        foreach (var summary in result.DockSummaries)
        {
            builder.AppendLine(string.Join(",",
                EscapeCsvField(summary.DockCode),
                summary.SplitUnsortedCount.ToString(),
                summary.FullCaseUnsortedCount.ToString(),
                summary.RecirculatedCount.ToString(),
                summary.ExceptionCount.ToString(),
                summary.SortedCount.ToString(),
                summary.SortedProgressPercent.ToString("0.##")));
        }

        return builder.ToString();
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static byte[] BuildUtf8BomCsvBytes(string csvContent)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        var preamble = Utf8EncodingWithBom.GetPreamble();
        var contentBytes = Utf8EncodingWithBom.GetBytes(csvContent);
        var bytes = new byte[preamble.Length + contentBytes.Length];
        Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
        Buffer.BlockCopy(contentBytes, 0, bytes, preamble.Length, contentBytes.Length);
        return bytes;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static string EscapeCsvField(string? value)
    {
        // 步骤：按既定流程执行当前方法逻辑。
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
}

