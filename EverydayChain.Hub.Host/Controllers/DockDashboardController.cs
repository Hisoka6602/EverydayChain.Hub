using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 码头看板查询接口。
/// </summary>
[ApiController]
[Route("api/v1/dock-dashboard")]
public sealed class DockDashboardController : ControllerBase
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
    /// </summary>
    /// <param name="request">查询请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>码头看板统计结果。</returns>
    [HttpPost("overview")]
    [ProducesResponseType(typeof(ApiResponse<DockDashboardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DockDashboardResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<DockDashboardResponse>>> QueryOverviewAsync([FromBody] DockDashboardQueryRequest request, CancellationToken cancellationToken)
    {
        var todayLocal = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Local);
        var start = request.StartTimeLocal ?? todayLocal;
        var end = request.EndTimeLocal ?? todayLocal.AddDays(1);

        if (!LocalDateTimeNormalizer.TryNormalize(start, "开始时间必须为本地时间，禁止传入 UTC 时间。", out var normalizedStart, out var startValidationMessage))
        {
            return BadRequest(ApiResponse<DockDashboardResponse>.Fail(startValidationMessage));
        }

        if (!LocalDateTimeNormalizer.TryNormalize(end, "结束时间必须为本地时间，禁止传入 UTC 时间。", out var normalizedEnd, out var endValidationMessage))
        {
            return BadRequest(ApiResponse<DockDashboardResponse>.Fail(endValidationMessage));
        }

        if (normalizedEnd <= normalizedStart)
        {
            return BadRequest(ApiResponse<DockDashboardResponse>.Fail("结束时间必须大于开始时间。"));
        }

        var result = await _dockDashboardQueryService.QueryAsync(new EverydayChain.Hub.Application.Models.DockDashboardQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd,
            WaveCode = request.WaveCode
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
