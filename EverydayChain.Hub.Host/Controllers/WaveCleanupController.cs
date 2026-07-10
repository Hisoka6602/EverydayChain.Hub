using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.WaveCleanup.Abstractions;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 提供波次清理查询、预演与执行接口。
/// </summary>
[ApiController]
[Route("api/v1/wave-cleanup")]
public sealed class WaveCleanupController : ControllerBase
{
    /// <summary>
    /// 存储波次清理服务。
    /// </summary>
    private readonly IWaveCleanupService _waveCleanupService;

    /// <summary>
    /// 存储波次查询服务。
    /// </summary>
    private readonly IWaveQueryService _waveQueryService;

    /// <summary>
    /// 初始化波次清理控制器。
    /// </summary>
    /// <param name="waveCleanupService">波次清理服务。</param>
    /// <param name="waveQueryService">波次查询服务。</param>
    public WaveCleanupController(IWaveCleanupService waveCleanupService, IWaveQueryService waveQueryService)
    {
        _waveCleanupService = waveCleanupService;
        _waveQueryService = waveQueryService;
    }

    /// <summary>
    /// 查询待清理波次明细。
    /// </summary>
    /// <param name="request">波次清理请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>待清理波次查询结果。</returns>
    [HttpPost("query")]
    [ProducesResponseType(typeof(ApiResponse<WaveCleanupQueryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WaveCleanupQueryResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<WaveCleanupQueryResponse>>> QueryAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] WaveCleanupRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _waveQueryService.QueryCleanupWaveAsync(request?.WaveCode, cancellationToken);
        var response = new WaveCleanupQueryResponse
        {
            Items = result.Items
                .Select(item => new WaveCleanupWaveItemResponse
                {
                    WaveId = item.WaveCode,
                    Remark = item.WaveRemark,
                    PackageTotal = item.PackageTotal,
                    SplitTotal = item.SplitTotal,
                    FullTotal = item.FullCaseTotal,
                    CreatedAt = item.CreatedTimeLocal,
                    Status = item.Status
                })
                .ToList()
        };
        return Ok(ApiResponse<WaveCleanupQueryResponse>.Success(response, "波次清理查询成功。"));
    }

    /// <summary>
    /// 预演执行波次清理。
    /// </summary>
    /// <param name="request">波次清理请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>预演执行结果。</returns>
    [HttpPost("dry-run")]
    [ProducesResponseType(typeof(ApiResponse<WaveCleanupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WaveCleanupResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<WaveCleanupResponse>>> DryRunAsync([FromBody] WaveCleanupRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.WaveCode))
        {
            return BadRequest(ApiResponse<WaveCleanupResponse>.Fail("波次号不能为空。"));
        }

        var result = await _waveCleanupService.DryRunByWaveCodeAsync(request.WaveCode.Trim(), cancellationToken);
        return Ok(ApiResponse<WaveCleanupResponse>.Success(BuildResponse(result), result.Message ?? string.Empty));
    }

    /// <summary>
    /// 正式执行波次清理。
    /// </summary>
    /// <param name="request">波次清理请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>正式执行结果。</returns>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(ApiResponse<WaveCleanupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WaveCleanupResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<WaveCleanupResponse>>> ExecuteAsync([FromBody] WaveCleanupRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.WaveCode))
        {
            return BadRequest(ApiResponse<WaveCleanupResponse>.Fail("波次号不能为空。"));
        }

        var result = await _waveCleanupService.ExecuteByWaveCodeAsync(
            request.WaveCode.Trim(),
            BuildExecuteContext(),
            cancellationToken);
        return Ok(ApiResponse<WaveCleanupResponse>.Success(BuildResponse(result), result.Message ?? string.Empty));
    }

    private WaveCleanupExecuteContext BuildExecuteContext()
    {
        var httpContext = HttpContext;
        var request = httpContext?.Request;

        return new WaveCleanupExecuteContext
        {
            TraceId = httpContext?.TraceIdentifier ?? string.Empty,
            RequestPath = request?.Path.Value ?? string.Empty,
            HttpMethod = request?.Method ?? string.Empty,
            OperatorId = request is null ? string.Empty : request.Headers["X-Operator-Id"].ToString().Trim(),
            ClientIp = httpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            UserAgent = request is null ? string.Empty : request.Headers.UserAgent.ToString().Trim()
        };
    }

    private static WaveCleanupResponse BuildResponse(WaveCleanupResult result)
    {
        return new WaveCleanupResponse
        {
            IdentifiedCount = result.IdentifiedCount,
            CleanedCount = result.CleanedCount,
            IsDryRun = result.IsDryRun
        };
    }
}
