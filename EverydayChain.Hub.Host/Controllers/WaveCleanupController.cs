using EverydayChain.Hub.Application.WaveCleanup.Abstractions;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 波次清理控制器，提供波次级 dry-run 评估与正式清理能力。
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
    /// 请求条件：波次号必填。
    /// 返回语义：返回识别数量与评估结果，不执行真实清理。
    /// </summary>
    /// <param name="request">波次清理请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>dry-run 评估结果，包含识别数量、清理数量与执行模式标识。</returns>
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
    /// 请求条件：波次号必填。
    /// 返回语义：返回识别数量与实际清理数量，表示正式清理执行结果。
    /// </summary>
    /// <param name="request">波次清理请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>正式清理执行结果，包含识别数量、已清理数量与执行模式标识。</returns>
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
