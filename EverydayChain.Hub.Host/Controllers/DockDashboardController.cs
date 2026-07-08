using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 提供码头看板查询与导出接口，用于按波次查看各码头的待分拣、已分拣、回流与异常情况。
/// </summary>
[ApiController]
[Route("api/v1/dock-dashboard")]
public sealed class DockDashboardController : QueryControllerBase
{
    /// <summary>
    /// 存储 Utf8EncodingWithBom 字段。
    /// </summary>
    private static readonly UTF8Encoding Utf8EncodingWithBom = new(true);

    /// <summary>
    /// 存储 _dockDashboardQueryService 字段。
    /// </summary>
    private readonly IDockDashboardQueryService _dockDashboardQueryService;

    /// <summary>
    /// 执行 DockDashboardController 方法。
    /// </summary>
    public DockDashboardController(IDockDashboardQueryService dockDashboardQueryService)
    {
        // 步骤：执行 DockDashboardController 方法的核心处理流程。
        _dockDashboardQueryService = dockDashboardQueryService;
    }

    /// <summary>
    /// 查询码头看板汇总，返回每个码头在指定时间范围或指定波次下的作业统计。
    /// </summary>
    /// <param name="request">请求体查询条件，支持按时间范围与波次筛选。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>码头看板汇总结果。</returns>
    [HttpPost("overview")]
    [ProducesResponseType(typeof(ApiResponse<DockDashboardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DockDashboardResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<DockDashboardResponse>>> QueryOverviewAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DockDashboardQueryRequest? request,
        [FromQuery] DockDashboardQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 QueryOverviewAsync 方法的核心处理流程。
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
    /// 导出码头看板 CSV 文件，包含各码头的待分拣量、已分拣量、回流量、异常量与进度。
    /// </summary>
    /// <param name="request">请求体查询条件。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>CSV 文件流。</returns>
    [HttpPost("export/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportCsvAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DockDashboardQueryRequest? request,
        [FromQuery] DockDashboardQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 ExportCsvAsync 方法的核心处理流程。
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
    /// 导出码头看板 Excel 文件，适合线下汇总、筛选与发送报表。
    /// </summary>
    /// <param name="request">请求体查询条件。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Excel 文件流。</returns>
    [HttpPost("export/xlsx")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportXlsxAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DockDashboardQueryRequest? request,
        [FromQuery] DockDashboardQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 ExportXlsxAsync 方法的核心处理流程。
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
    /// 执行 QueryInternalAsync 方法。
    /// </summary>
    private async Task<(EverydayChain.Hub.Application.Models.DockDashboardQueryResult? Value, ActionResult? Result)> QueryInternalAsync(
        DockDashboardQueryRequest? request,
        DockDashboardQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 QueryInternalAsync 方法的核心处理流程。
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
    /// 执行 BuildCsv 方法。
    /// </summary>
    private static string BuildCsv(EverydayChain.Hub.Application.Models.DockDashboardQueryResult result)
    {
        // 步骤：执行 BuildCsv 方法的核心处理流程。
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
    /// 执行 BuildUtf8BomCsvBytes 方法。
    /// </summary>
    private static byte[] BuildUtf8BomCsvBytes(string csvContent)
    {
        // 步骤：执行 BuildUtf8BomCsvBytes 方法的核心处理流程。
        var preamble = Utf8EncodingWithBom.GetPreamble();
        var contentBytes = Utf8EncodingWithBom.GetBytes(csvContent);
        var bytes = new byte[preamble.Length + contentBytes.Length];
        Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
        Buffer.BlockCopy(contentBytes, 0, bytes, preamble.Length, contentBytes.Length);
        return bytes;
    }

    /// <summary>
    /// 执行 EscapeCsvField 方法。
    /// </summary>
    private static string EscapeCsvField(string? value)
    {
        // 步骤：执行 EscapeCsvField 方法的核心处理流程。
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

