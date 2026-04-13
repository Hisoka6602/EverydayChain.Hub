using Microsoft.AspNetCore.Mvc;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 扫描上传接口。
/// </summary>
[ApiController]
[Route("api/v1/scan")]
public sealed class ScanController : ControllerBase {
    /// <summary>
    /// 扫描上传应用服务。
    /// </summary>
    private readonly IScanIngressService scanIngressService;

    /// <summary>
    /// 初始化扫描上传控制器。
    /// </summary>
    /// <param name="scanIngressService">扫描上传应用服务。</param>
    public ScanController(IScanIngressService scanIngressService) {
        this.scanIngressService = scanIngressService;
    }

    /// <summary>
    /// 接收扫描上传请求。
    /// </summary>
    /// <param name="request">扫描上传请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>受理结果。</returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(ApiResponse<ScanUploadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ScanUploadResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ScanUploadResponse>>> UploadAsync([FromBody] ScanUploadRequest request, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(request.Barcode)) {
            return BadRequest(ApiResponse<ScanUploadResponse>.Fail("条码不能为空。"));
        }

        if (string.IsNullOrWhiteSpace(request.DeviceCode)) {
            return BadRequest(ApiResponse<ScanUploadResponse>.Fail("设备编码不能为空。"));
        }

        if (request.ScanTimeLocal.Kind == DateTimeKind.Utc) {
            return BadRequest(ApiResponse<ScanUploadResponse>.Fail("扫描时间必须为本地时间，禁止传入 UTC 时间。"));
        }

        var normalizedScanTime = NormalizeLocalTime(request.ScanTimeLocal);
        var applicationResult = await scanIngressService.ExecuteAsync(new ScanUploadApplicationRequest {
            Barcode = request.Barcode.Trim(),
            DeviceCode = request.DeviceCode.Trim(),
            ScanTimeLocal = normalizedScanTime
        }, cancellationToken);

        var response = new ScanUploadResponse {
            IsAccepted = applicationResult.IsAccepted,
            TaskCode = applicationResult.TaskCode
        };

        return Ok(ApiResponse<ScanUploadResponse>.Success(response, applicationResult.Message));
    }

    /// <summary>
    /// 规范化本地时间输入。
    /// </summary>
    /// <param name="candidateTime">候选时间。</param>
    /// <returns>规范化后的本地时间。</returns>
    private static DateTime NormalizeLocalTime(DateTime candidateTime) {
        if (candidateTime == DateTime.MinValue) {
            return DateTime.Now;
        }

        if (candidateTime.Kind == DateTimeKind.Unspecified) {
            return DateTime.SpecifyKind(candidateTime, DateTimeKind.Local);
        }

        return candidateTime;
    }
}
