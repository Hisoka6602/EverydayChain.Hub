using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 总看板查询接口。
/// </summary>
[ApiController]
[Route("api/v1/dashboard")]
public sealed class GlobalDashboardController : ControllerBase
{
    /// <summary>
    /// 总看板查询服务。
    /// </summary>
    private readonly IGlobalDashboardQueryService _globalDashboardQueryService;

    /// <summary>
    /// 初始化总看板控制器。
    /// </summary>
    /// <param name="globalDashboardQueryService">总看板查询服务。</param>
    public GlobalDashboardController(IGlobalDashboardQueryService globalDashboardQueryService)
    {
        _globalDashboardQueryService = globalDashboardQueryService;
    }

    /// <summary>
    /// 查询总看板统计。
    /// </summary>
    /// <param name="request">查询请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>总看板统计结果。</returns>
    [HttpPost("overview")]
    [ProducesResponseType(typeof(ApiResponse<GlobalDashboardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GlobalDashboardResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<GlobalDashboardResponse>>> QueryOverviewAsync([FromBody] GlobalDashboardQueryRequest request, CancellationToken cancellationToken)
    {
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(request.StartTimeLocal, request.EndTimeLocal, out var normalizedStartTime, out var normalizedEndTime, out var validationMessage))
        {
            return BadRequest(ApiResponse<GlobalDashboardResponse>.Fail(validationMessage));
        }

        var result = await _globalDashboardQueryService.QueryAsync(new EverydayChain.Hub.Application.Models.GlobalDashboardQueryRequest
        {
            StartTimeLocal = normalizedStartTime,
            EndTimeLocal = normalizedEndTime
        }, cancellationToken);
        var response = BuildResponse(result);
        return Ok(ApiResponse<GlobalDashboardResponse>.Success(response, "总看板查询成功。"));
    }

    /// <summary>
    /// 构建总看板响应模型。
    /// </summary>
    /// <param name="result">应用层查询结果。</param>
    /// <returns>Host 层响应模型。</returns>
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
}
