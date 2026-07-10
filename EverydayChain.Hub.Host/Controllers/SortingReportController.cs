using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 提供分拣报表查询与导出接口，用于按码头查看拆零、整件、回流与异常的统计结果。
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
    /// 存储 _sortingReportQueryService 字段。
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
    /// 查询分拣报表，返回指定时间段内各码头的拆零件量、整件量、已分拣量、回流量与异常量。
    /// </summary>
    /// <param name="request">请求体查询条件，支持按时间范围与码头筛选。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>分拣报表结果。</returns>
    [HttpPost("query")]
    [ProducesResponseType(typeof(ApiResponse<SortingReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SortingReportResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<SortingReportResponse>>> QueryAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SortingReportQueryRequest? request,
        [FromQuery] SortingReportQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 QueryAsync 方法的核心处理流程。
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
    /// 导出分拣报表 CSV 文件，适用于离线核对码头维度的统计结果。
    /// </summary>
    /// <param name="request">请求体查询条件。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>CSV 文件流。</returns>
    [HttpPost("export/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportCsvAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SortingReportQueryRequest? request,
        [FromQuery] SortingReportQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 ExportCsvAsync 方法的核心处理流程。
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
    /// 导出分拣报表 Excel 文件，适用于报表沉淀与二次加工。
    /// </summary>
    /// <param name="request">请求体查询条件。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Excel 文件流。</returns>
    [HttpPost("export/xlsx")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportXlsxAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SortingReportQueryRequest? request,
        [FromQuery] SortingReportQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 ExportXlsxAsync 方法的核心处理流程。
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
            "分拣报表",
            ["码头号", "拆零总数", "整件总数", "拆零分拣数", "整件分拣数", "回流数", "异常数"],
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
}

