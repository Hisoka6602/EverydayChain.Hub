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
[Route("api/v1/reports")]
public sealed class SortingReportController : QueryControllerBase
{
    /// <summary>
    /// 存储带 BOM 的 UTF-8 编码器。
    /// </summary>
    private static readonly UTF8Encoding Utf8EncodingWithBom = new(true);

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly ISortingReportQueryService _sortingReportQueryService;

    /// <summary>
    /// 初始化分拣报表控制器。
    /// </summary>
    /// <param name="sortingReportQueryService">分拣报表查询服务。</param>
    public SortingReportController(ISortingReportQueryService sortingReportQueryService)
    {
        _sortingReportQueryService = sortingReportQueryService;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [HttpPost("query")]
    [ProducesResponseType(typeof(ApiResponse<SortingReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SortingReportResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<SortingReportResponse>>> QueryAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SortingReportQueryRequest? request,
        [FromQuery] SortingReportQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        var resolvedRequest = ResolveRequest(request, queryRequest);
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(resolvedRequest.StartTimeLocal, resolvedRequest.EndTimeLocal, out var normalizedStart, out var normalizedEnd, out var validationMessage))
        {
            return BadRequest(ApiResponse<SortingReportResponse>.Fail(validationMessage));
        }

        var result = await _sortingReportQueryService.QueryAsync(new EverydayChain.Hub.Application.Models.SortingReportQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd,
            DockCode = resolvedRequest.DockCode
        }, cancellationToken);

        var response = new SortingReportResponse
        {
            StartTimeLocal = result.StartTimeLocal,
            EndTimeLocal = result.EndTimeLocal,
            SelectedDockCode = result.SelectedDockCode,
            Rows = result.Rows
                .Select(row => new SortingReportRowResponse
                {
                    DockCode = row.DockCode,
                    SplitTotalCount = row.SplitTotalCount,
                    FullCaseTotalCount = row.FullCaseTotalCount,
                    SplitSortedCount = row.SplitSortedCount,
                    FullCaseSortedCount = row.FullCaseSortedCount,
                    RecirculatedCount = row.RecirculatedCount,
                    ExceptionCount = row.ExceptionCount
                })
                .ToList()
        };

        return Ok(ApiResponse<SortingReportResponse>.Success(response, "分拣报表查询成功。"));
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [HttpPost("export/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportCsvAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SortingReportQueryRequest? request,
        [FromQuery] SortingReportQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        var resolvedRequest = ResolveRequest(request, queryRequest);
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(resolvedRequest.StartTimeLocal, resolvedRequest.EndTimeLocal, out var normalizedStart, out var normalizedEnd, out var validationMessage))
        {
            return BadRequest(ApiResponse<object>.Fail(validationMessage));
        }

        var csvContent = await _sortingReportQueryService.ExportCsvAsync(new EverydayChain.Hub.Application.Models.SortingReportQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd,
            DockCode = resolvedRequest.DockCode
        }, cancellationToken);

        var localNow = DateTimeOffset.Now.LocalDateTime;
        var fileName = $"sorting-report-{localNow:yyyyMMddHHmmss}.csv";
        var csvBytes = BuildUtf8BomCsvBytes(csvContent);
        return File(csvBytes, "text/csv; charset=utf-8", fileName);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [HttpPost("export/xlsx")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportXlsxAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SortingReportQueryRequest? request,
        [FromQuery] SortingReportQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        var resolvedRequest = ResolveRequest(request, queryRequest);
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(resolvedRequest.StartTimeLocal, resolvedRequest.EndTimeLocal, out var normalizedStart, out var normalizedEnd, out var validationMessage))
        {
            return BadRequest(ApiResponse<object>.Fail(validationMessage));
        }

        var result = await _sortingReportQueryService.QueryAsync(new EverydayChain.Hub.Application.Models.SortingReportQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd,
            DockCode = resolvedRequest.DockCode
        }, cancellationToken);

        var content = SimpleXlsxBuilder.BuildSingleSheet(
            "SortingReport",
            ["DockCode", "SplitTotalCount", "FullCaseTotalCount", "SplitSortedCount", "FullCaseSortedCount", "RecirculatedCount", "ExceptionCount"],
            result.Rows
                .Select(row => (IReadOnlyList<string?>)
                [
                    row.DockCode,
                    row.SplitTotalCount.ToString(),
                    row.FullCaseTotalCount.ToString(),
                    row.SplitSortedCount.ToString(),
                    row.FullCaseSortedCount.ToString(),
                    row.RecirculatedCount.ToString(),
                    row.ExceptionCount.ToString()
                ])
                .ToList());

        var localNow = DateTimeOffset.Now.LocalDateTime;
        var fileName = $"sorting-report-{localNow:yyyyMMddHHmmss}.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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
}

