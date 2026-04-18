using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 分拣报表控制器，提供报表查询与 CSV 导出能力。
/// </summary>
[ApiController]
[Route("api/v1/reports")]
public sealed class SortingReportController : QueryControllerBase
{
    /// <summary>
    /// 带 BOM 的 UTF-8 编码实例。
    /// </summary>
    private static readonly UTF8Encoding Utf8EncodingWithBom = new(true);

    /// <summary>
    /// 分拣报表查询服务。
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
    /// 查询分拣报表。
    /// 请求条件：开始时间与结束时间必填，且结束时间大于开始时间。
    /// 返回语义：返回按码头聚合的分拣统计行集合；参数非法返回 400。
    /// </summary>
    /// <param name="request">请求体查询请求。</param>
    /// <param name="queryRequest">查询字符串请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>分拣报表查询结果，包含生效查询窗口、码头筛选与报表明细行。</returns>
    [HttpPost("query")]
    [ProducesResponseType(typeof(ApiResponse<SortingReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SortingReportResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<SortingReportResponse>>> QueryAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SortingReportQueryRequest? request,
        [FromQuery] SortingReportQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
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
    /// 导出 CSV 报表。
    /// 请求条件：时间范围校验规则与报表查询一致。
    /// 返回语义：成功返回 UTF-8 BOM 编码的 CSV 文件流；参数校验失败返回 400；系统异常返回结果遵循项目实际异常处理策略。
    /// </summary>
    /// <param name="request">请求体查询请求。</param>
    /// <param name="queryRequest">查询字符串请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>分拣报表 CSV 文件流或参数校验失败结果。</returns>
    [HttpPost("export/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportCsvAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SortingReportQueryRequest? request,
        [FromQuery] SortingReportQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
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
    /// 构建带 UTF-8 BOM 的 CSV 字节数组。
    /// </summary>
    /// <param name="csvContent">CSV 文本内容。</param>
    /// <returns>带 UTF-8 BOM 的字节数组。</returns>
    private static byte[] BuildUtf8BomCsvBytes(string csvContent)
    {
        var preamble = Utf8EncodingWithBom.GetPreamble();
        var contentBytes = Utf8EncodingWithBom.GetBytes(csvContent);
        var bytes = new byte[preamble.Length + contentBytes.Length];
        Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
        Buffer.BlockCopy(contentBytes, 0, bytes, preamble.Length, contentBytes.Length);
        return bytes;
    }
}
