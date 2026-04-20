using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 波次查询控制器，提供波次选项、波次摘要与波次分区明细查询能力。
/// </summary>
[ApiController]
[Route("api/v1/waves")]
public sealed class WavesController(IWaveQueryService waveQueryService) : QueryControllerBase
{
    /// <summary>
    /// 查询时间区间内的波次选项。
    /// </summary>
    /// <param name="request">请求体查询请求。</param>
    /// <param name="queryRequest">查询字符串请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>波次选项结果。</returns>
    [HttpPost("options")]
    [ProducesResponseType(typeof(ApiResponse<WaveOptionsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WaveOptionsResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<WaveOptionsResponse>>> QueryOptionsAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] WaveOptionsQueryRequest? request,
        [FromQuery] WaveOptionsQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        var resolvedRequest = ResolveRequest(request, queryRequest);
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(
                resolvedRequest.StartTimeLocal,
                resolvedRequest.EndTimeLocal,
                out var normalizedStart,
                out var normalizedEnd,
                out var validationMessage))
        {
            return BadRequest(ApiResponse<WaveOptionsResponse>.Fail(validationMessage));
        }

        var result = await waveQueryService.QueryOptionsAsync(new EverydayChain.Hub.Application.Models.WaveOptionsQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd
        }, cancellationToken);
        var response = new WaveOptionsResponse
        {
            StartTimeLocal = result.StartTimeLocal,
            EndTimeLocal = result.EndTimeLocal,
            WaveOptions = result.WaveOptions
                .Select(item => new WaveOptionItemResponse
                {
                    WaveCode = item.WaveCode,
                    WaveRemark = item.WaveRemark
                })
                .ToList()
        };
        return Ok(ApiResponse<WaveOptionsResponse>.Success(response, "波次选项查询成功。"));
    }

    /// <summary>
    /// 查询单个波次摘要。
    /// </summary>
    /// <param name="request">请求体查询请求。</param>
    /// <param name="queryRequest">查询字符串请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>波次摘要结果。</returns>
    [HttpPost("summary")]
    [ProducesResponseType(typeof(ApiResponse<WaveSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WaveSummaryResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<WaveSummaryResponse>>> QuerySummaryAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] WaveSummaryQueryRequest? request,
        [FromQuery] WaveSummaryQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        var resolvedRequest = ResolveRequest(request, queryRequest);
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(
                resolvedRequest.StartTimeLocal,
                resolvedRequest.EndTimeLocal,
                out var normalizedStart,
                out var normalizedEnd,
                out var validationMessage))
        {
            return BadRequest(ApiResponse<WaveSummaryResponse>.Fail(validationMessage));
        }

        if (string.IsNullOrWhiteSpace(resolvedRequest.WaveCode))
        {
            return BadRequest(ApiResponse<WaveSummaryResponse>.Fail("WaveCode 不能为空白。"));
        }

        var result = await waveQueryService.QuerySummaryAsync(new EverydayChain.Hub.Application.Models.WaveSummaryQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd,
            WaveCode = resolvedRequest.WaveCode.Trim()
        }, cancellationToken);
        if (result is null)
        {
            return BadRequest(ApiResponse<WaveSummaryResponse>.Fail($"未找到波次 [{resolvedRequest.WaveCode.Trim()}] 在指定时间范围内的数据。"));
        }

        var response = new WaveSummaryResponse
        {
            WaveCode = result.WaveCode,
            WaveRemark = result.WaveRemark,
            TotalCount = result.TotalCount,
            UnsortedCount = result.UnsortedCount,
            SortedProgressPercent = result.SortedProgressPercent,
            RecirculatedCount = result.RecirculatedCount,
            ExceptionCount = result.ExceptionCount
        };
        return Ok(ApiResponse<WaveSummaryResponse>.Success(response, "波次摘要查询成功。"));
    }

    /// <summary>
    /// 查询单个波次分区明细。
    /// </summary>
    /// <param name="request">请求体查询请求。</param>
    /// <param name="queryRequest">查询字符串请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>波次分区结果。</returns>
    [HttpPost("zones")]
    [ProducesResponseType(typeof(ApiResponse<WaveZoneResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WaveZoneResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<WaveZoneResponse>>> QueryZonesAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] WaveZoneQueryRequest? request,
        [FromQuery] WaveZoneQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        var resolvedRequest = ResolveRequest(request, queryRequest);
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(
                resolvedRequest.StartTimeLocal,
                resolvedRequest.EndTimeLocal,
                out var normalizedStart,
                out var normalizedEnd,
                out var validationMessage))
        {
            return BadRequest(ApiResponse<WaveZoneResponse>.Fail(validationMessage));
        }

        if (string.IsNullOrWhiteSpace(resolvedRequest.WaveCode))
        {
            return BadRequest(ApiResponse<WaveZoneResponse>.Fail("WaveCode 不能为空白。"));
        }

        var result = await waveQueryService.QueryZonesAsync(new EverydayChain.Hub.Application.Models.WaveZoneQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd,
            WaveCode = resolvedRequest.WaveCode.Trim()
        }, cancellationToken);
        if (result is null)
        {
            return BadRequest(ApiResponse<WaveZoneResponse>.Fail($"未找到波次 [{resolvedRequest.WaveCode.Trim()}] 在指定时间范围内的数据。"));
        }

        var response = new WaveZoneResponse
        {
            WaveCode = result.WaveCode,
            WaveRemark = result.WaveRemark,
            Zones = result.Zones
                .Select(zone => new WaveZoneSummaryResponse
                {
                    ZoneCode = zone.ZoneCode,
                    ZoneName = zone.ZoneName,
                    TotalCount = zone.TotalCount,
                    UnsortedCount = zone.UnsortedCount,
                    SortedProgressPercent = zone.SortedProgressPercent,
                    RecirculatedCount = zone.RecirculatedCount,
                    ExceptionCount = zone.ExceptionCount
                })
                .ToList()
        };
        return Ok(ApiResponse<WaveZoneResponse>.Success(response, "波次分区明细查询成功。"));
    }
}
