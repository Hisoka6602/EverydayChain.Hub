using EverydayChain.Hub.Application.WaveCleanup.Abstractions;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 波次清理接口。
/// </summary>
[ApiController]
[Route("api/v1/wave-cleanup")]
public sealed class WaveCleanupController : ControllerBase {
    /// <summary>
    /// 波次清理应用服务。
    /// </summary>
    private readonly IWaveCleanupService _waveCleanupService;

    /// <summary>
    /// 初始化波次清理控制器。
    /// </summary>
    /// <param name="waveCleanupService">波次清理应用服务。</param>
    public WaveCleanupController(IWaveCleanupService waveCleanupService) {
        _waveCleanupService = waveCleanupService;
    }

    /// <summary>
    /// 按波次执行 dry-run 清理评估。
    /// </summary>
    /// <param name="request">波次清理请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>评估结果。</returns>
    [HttpPost("dry-run")]
    [ProducesResponseType(typeof(ApiResponse<WaveCleanupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WaveCleanupResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<WaveCleanupResponse>>> DryRunAsync([FromBody] WaveCleanupRequest request, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(request.WaveCode)) {
            return BadRequest(ApiResponse<WaveCleanupResponse>.Fail("波次号不能为空。"));
        }

        var result = await _waveCleanupService.DryRunByWaveCodeAsync(request.WaveCode.Trim(), cancellationToken);
        var response = BuildResponse(result);
        return Ok(ApiResponse<WaveCleanupResponse>.Success(response, result.Message ?? string.Empty));
    }

    /// <summary>
    /// 按波次执行正式清理。
    /// </summary>
    /// <param name="request">波次清理请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>执行结果。</returns>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(ApiResponse<WaveCleanupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WaveCleanupResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<WaveCleanupResponse>>> ExecuteAsync([FromBody] WaveCleanupRequest request, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(request.WaveCode)) {
            return BadRequest(ApiResponse<WaveCleanupResponse>.Fail("波次号不能为空。"));
        }

        var result = await _waveCleanupService.ExecuteByWaveCodeAsync(request.WaveCode.Trim(), cancellationToken);
        var response = BuildResponse(result);
        return Ok(ApiResponse<WaveCleanupResponse>.Success(response, result.Message ?? string.Empty));
    }

    /// <summary>
    /// 构建控制器响应模型。
    /// </summary>
    /// <param name="result">应用服务结果。</param>
    /// <returns>接口响应模型。</returns>
    private static WaveCleanupResponse BuildResponse(WaveCleanupResult result) {
        return new WaveCleanupResponse {
            IdentifiedCount = result.IdentifiedCount,
            CleanedCount = result.CleanedCount,
            IsDryRun = result.IsDryRun
        };
    }
}
