using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 扫描上传控制器，负责接收设备扫码数据并触发任务受理流程。
/// </summary>
[ApiController]
[Route("api/v1/scan")]
public sealed class ScanController : ControllerBase {
    /// <summary>
    /// 当请求体为空时返回的错误消息。
    /// </summary>
    private const string EmptyRequestBodyMessage = "扫描上传请求体不能为空。";

    /// <summary>
    /// 多条码场景下非首条条码的尺寸与重量回写值。
    /// </summary>
    private const decimal MultiBarcodeFallbackMeasurementValue = 0M;

    /// <summary>
    /// 条码最大长度限制。
    /// </summary>
    private const int MaxBarcodeLength = 128;

    /// <summary>
    /// 单次请求允许提交的最大条码数量。
    /// </summary>
    private const int MaxBarcodeCountPerRequest = 100;

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
    /// 接收扫描上传请求并批量处理条码。
    /// 请求条件：<see cref="ScanUploadRequest.DeviceCode"/> 与 <see cref="ScanUploadRequest.Barcodes"/> 必填。
    /// 返回语义：全部条码受理成功返回 200；存在任意条码失败返回 400 且返回逐条处理结果。
    /// </summary>
    /// <param name="request">扫描上传请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>扫描受理结果集合，包含每个条码的受理状态、任务编码与失败语义码。</returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ScanUploadResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ScanUploadResponse>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ScanUploadResponse>>>> UploadAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ScanUploadRequest? request, CancellationToken cancellationToken) {
        if (request is null) {
            return BadRequest(ApiResponse<IReadOnlyList<ScanUploadResponse>>.Fail(EmptyRequestBodyMessage));
        }

        if (!TryBuildBarcodes(request, out var barcodes, out var barcodeValidationMessage)) {
            return BadRequest(ApiResponse<IReadOnlyList<ScanUploadResponse>>.Fail(barcodeValidationMessage));
        }

        if (string.IsNullOrWhiteSpace(request.DeviceCode)) {
            return BadRequest(ApiResponse<IReadOnlyList<ScanUploadResponse>>.Fail("设备编码不能为空。"));
        }

        if (!LocalDateTimeNormalizer.TryNormalize(request.ScanTimeLocal, "扫描时间必须为本地时间，禁止传入 UTC 时间。", out var normalizedScanTime, out var validationMessage)) {
            return BadRequest(ApiResponse<IReadOnlyList<ScanUploadResponse>>.Fail(validationMessage));
        }

        var responses = new List<ScanUploadResponse>(barcodes.Count);
        var hasRejected = false;
        var firstFailureMessage = string.Empty;
        for (var i = 0; i < barcodes.Count; i++) {
            var isPrimaryBarcode = i == 0;
            var applicationResult = await scanIngressService.ExecuteAsync(new ScanUploadApplicationRequest {
                Barcode = barcodes[i],
                DeviceCode = request.DeviceCode.Trim(),
                ScanTimeLocal = normalizedScanTime,
                TraceId = (request.TraceId ?? string.Empty).Trim(),
                LengthMm = isPrimaryBarcode ? request.LengthMm : MultiBarcodeFallbackMeasurementValue,
                WidthMm = isPrimaryBarcode ? request.WidthMm : MultiBarcodeFallbackMeasurementValue,
                HeightMm = isPrimaryBarcode ? request.HeightMm : MultiBarcodeFallbackMeasurementValue,
                VolumeMm3 = isPrimaryBarcode ? request.VolumeMm3 : MultiBarcodeFallbackMeasurementValue,
                WeightGram = isPrimaryBarcode ? request.WeightGram : MultiBarcodeFallbackMeasurementValue
            }, cancellationToken);

            responses.Add(new ScanUploadResponse {
                IsAccepted = applicationResult.IsAccepted,
                TaskCode = applicationResult.TaskCode,
                BarcodeType = applicationResult.BarcodeType,
                FailureReason = applicationResult.FailureReason
            });

            if (!applicationResult.IsAccepted) {
                hasRejected = true;
                if (string.IsNullOrWhiteSpace(firstFailureMessage)) {
                    firstFailureMessage = string.IsNullOrWhiteSpace(applicationResult.Message)
                        ? applicationResult.FailureReason
                        : applicationResult.Message;
                }
            }
        }

        if (hasRejected) {
            return BadRequest(ApiResponse<IReadOnlyList<ScanUploadResponse>>.Fail(firstFailureMessage, responses));
        }

        return Ok(ApiResponse<IReadOnlyList<ScanUploadResponse>>.Success(responses, $"扫描请求已受理，共处理 {responses.Count} 个条码。"));
    }

    /// <summary>
    /// 统一整理请求中的条码列表。
    /// </summary>
    /// <param name="request">扫描上传请求。</param>
    /// <param name="normalizedBarcodes">整理后的条码列表。</param>
    /// <param name="validationMessage">条码校验失败信息。</param>
    /// <returns>条码是否通过校验。</returns>
    private static bool TryBuildBarcodes(ScanUploadRequest request, out List<string> normalizedBarcodes, out string validationMessage) {
        validationMessage = string.Empty;
        normalizedBarcodes = [];
        var requestBarcodes = request.Barcodes ?? [];
        if (requestBarcodes.Count == 0) {
            validationMessage = "条码不能为空。";
            return false;
        }

        if (requestBarcodes.Count > MaxBarcodeCountPerRequest) {
            validationMessage = $"单次最多允许提交 {MaxBarcodeCountPerRequest} 个条码。";
            return false;
        }

        foreach (var barcode in requestBarcodes) {
            if (string.IsNullOrWhiteSpace(barcode)) {
                validationMessage = "条码列表中存在空条码，请检查后重试。";
                return false;
            }

            var trimmedBarcode = barcode.Trim();
            if (trimmedBarcode.Length > MaxBarcodeLength) {
                validationMessage = $"条码长度不能超过 {MaxBarcodeLength}。";
                return false;
            }

            normalizedBarcodes.Add(trimmedBarcode);
        }

        return true;
    }
}
