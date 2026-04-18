using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 码头看板查询控制器，提供码头维度分拣进度与异常指标查询能力。
/// </summary>
[ApiController]
[Route("api/v1/dock-dashboard")]
public sealed class DockDashboardController : QueryControllerBase
{
    /// <summary>
    /// 码头看板查询服务。
    /// </summary>
    private readonly IDockDashboardQueryService _dockDashboardQueryService;

    /// <summary>
    /// 初始化码头看板控制器。
    /// </summary>
    /// <param name="dockDashboardQueryService">码头看板查询服务。</param>
    public DockDashboardController(IDockDashboardQueryService dockDashboardQueryService)
    {
        _dockDashboardQueryService = dockDashboardQueryService;
    }

    /// <summary>
    /// 查询码头看板统计。
    /// 请求条件：时间参数可选；传入时必须满足结束时间大于开始时间且为本地时间语义。
    /// 返回语义：返回码头维度统计、波次筛选选项与实际生效时间窗口。
    /// </summary>
    /// <param name="request">请求体查询请求。</param>
    /// <param name="queryRequest">查询字符串请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>码头看板统计结果，包含码头汇总集合与当前波次筛选信息。</returns>
    [HttpPost("overview")]
    [ProducesResponseType(typeof(ApiResponse<DockDashboardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DockDashboardResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<DockDashboardResponse>>> QueryOverviewAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DockDashboardQueryRequest? request,
        [FromQuery] DockDashboardQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
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
}
