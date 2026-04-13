using Microsoft.AspNetCore.Mvc;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;

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

        if (!LocalDateTimeNormalizer.TryNormalize(request.ScanTimeLocal, "扫描时间必须为本地时间，禁止传入 UTC 时间。", out var normalizedScanTime, out var validationMessage)) {
            return BadRequest(ApiResponse<ScanUploadResponse>.Fail(validationMessage));
        }

        var applicationResult = await scanIngressService.ExecuteAsync(new ScanUploadApplicationRequest {
            Barcode = request.Barcode.Trim(),
            DeviceCode = request.DeviceCode.Trim(),
            ScanTimeLocal = normalizedScanTime,
            TraceId = (request.TraceId ?? string.Empty).Trim(),
            LengthMm = request.LengthMm,
            WidthMm = request.WidthMm,
            HeightMm = request.HeightMm,
            VolumeMm3 = request.VolumeMm3,
            WeightGram = request.WeightGram
        }, cancellationToken);

        var response = new ScanUploadResponse {
            IsAccepted = applicationResult.IsAccepted,
            TaskCode = applicationResult.TaskCode,
            BarcodeType = applicationResult.BarcodeType,
            FailureReason = applicationResult.FailureReason
        };

        if (!applicationResult.IsAccepted) {
            var failureMessage = string.IsNullOrWhiteSpace(applicationResult.Message)
                ? applicationResult.FailureReason
                : applicationResult.Message;
            return BadRequest(ApiResponse<ScanUploadResponse>.Fail(failureMessage, response));
        }

        return Ok(ApiResponse<ScanUploadResponse>.Success(response, applicationResult.Message));
    }
}
