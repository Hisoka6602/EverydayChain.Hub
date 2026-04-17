using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 分拣报表查询与导出接口。
/// </summary>
[ApiController]
[Route("api/v1/reports")]
public sealed class SortingReportController : ControllerBase
{
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
    /// </summary>
    /// <param name="request">查询请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>分拣报表查询结果。</returns>
    [HttpPost("query")]
    [ProducesResponseType(typeof(ApiResponse<SortingReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SortingReportResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<SortingReportResponse>>> QueryAsync([FromBody] SortingReportQueryRequest request, CancellationToken cancellationToken)
    {
        if (!TryValidateTimeRange(request.StartTimeLocal, request.EndTimeLocal, out var normalizedStart, out var normalizedEnd, out var validationResult))
        {
            return validationResult!;
        }

        var result = await _sortingReportQueryService.QueryAsync(new EverydayChain.Hub.Application.Models.SortingReportQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd,
            DockCode = request.DockCode
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
    /// </summary>
    /// <param name="request">查询请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>CSV 文件。</returns>
    [HttpPost("export/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportCsvAsync([FromBody] SortingReportQueryRequest request, CancellationToken cancellationToken)
    {
        if (!TryValidateTimeRange(request.StartTimeLocal, request.EndTimeLocal, out var normalizedStart, out var normalizedEnd, out var validationResult))
        {
            return validationResult!;
        }

        var csvContent = await _sortingReportQueryService.ExportCsvAsync(new EverydayChain.Hub.Application.Models.SortingReportQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd,
            DockCode = request.DockCode
        }, cancellationToken);

        var fileName = $"sorting-report-{DateTime.Now:yyyyMMddHHmmss}.csv";
        return File(Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
    }

    /// <summary>
    /// 校验并规范化时间区间。
    /// </summary>
    /// <param name="startTimeLocal">开始时间。</param>
    /// <param name="endTimeLocal">结束时间。</param>
    /// <param name="normalizedStart">规范化开始时间。</param>
    /// <param name="normalizedEnd">规范化结束时间。</param>
    /// <param name="validationResult">校验失败结果。</param>
    /// <returns>是否通过校验。</returns>
    private bool TryValidateTimeRange(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        out DateTime normalizedStart,
        out DateTime normalizedEnd,
        out BadRequestObjectResult? validationResult)
    {
        normalizedStart = default;
        normalizedEnd = default;
        validationResult = null;

        if (startTimeLocal == DateTime.MinValue)
        {
            validationResult = BadRequest(ApiResponse<object>.Fail("开始时间不能为空。"));
            return false;
        }

        if (endTimeLocal == DateTime.MinValue)
        {
            validationResult = BadRequest(ApiResponse<object>.Fail("结束时间不能为空。"));
            return false;
        }

        if (!LocalDateTimeNormalizer.TryNormalize(startTimeLocal, "开始时间必须为本地时间，禁止传入 UTC 时间。", out normalizedStart, out var startValidationMessage))
        {
            validationResult = BadRequest(ApiResponse<object>.Fail(startValidationMessage));
            return false;
        }

        if (!LocalDateTimeNormalizer.TryNormalize(endTimeLocal, "结束时间必须为本地时间，禁止传入 UTC 时间。", out normalizedEnd, out var endValidationMessage))
        {
            validationResult = BadRequest(ApiResponse<object>.Fail(endValidationMessage));
            return false;
        }

        if (normalizedEnd <= normalizedStart)
        {
            validationResult = BadRequest(ApiResponse<object>.Fail("结束时间必须大于开始时间。"));
            return false;
        }

        return true;
    }
}
