using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 提供扫描上传接口，用于接收设备侧条码扫描结果并驱动任务识别、落格与读码率统计。
/// </summary>
[ApiController]
[Route("api/v1/scan")]
public sealed class ScanController : ControllerBase {
    /// <summary>
    /// 存储 EmptyRequestBodyMessage 字段。
    /// </summary>
    private const string EmptyRequestBodyMessage = "扫描上传请求体不能为空。";

    /// <summary>
    /// 存储 UnresolvableChuteMessage 字段。
    /// </summary>
    private const string UnresolvableChuteMessage = "扫描 barcodes 内不能包含无法解析格口的条码。";

    /// <summary>
    /// 存储 MultipleChutesMessage 字段。
    /// </summary>
    private const string MultipleChutesMessage = "扫描 barcodes 不能包含多个格口的条码。";

    /// <summary>
    /// 存储 MultiBarcodeFallbackMeasurementValue 字段。
    /// </summary>
    private const decimal MultiBarcodeFallbackMeasurementValue = 0M;

    /// <summary>
    /// 存储 MaxBarcodeLength 字段。
    /// </summary>
    private const int MaxBarcodeLength = 128;

    /// <summary>
    /// 存储 MaxBarcodeCountPerRequest 字段。
    /// </summary>
    private const int MaxBarcodeCountPerRequest = 100;

    /// <summary>
    /// 存储 scanIngressService 字段。
    /// </summary>
    private readonly IScanIngressService scanIngressService;

    /// <summary>
    /// 存储 barcodeParser 字段。
    /// </summary>
    private readonly IBarcodeParser barcodeParser;

    /// <summary>
    /// 执行 ScanController 方法。
    /// </summary>
    public ScanController(IScanIngressService scanIngressService, IBarcodeParser barcodeParser) {
        // 步骤：执行 ScanController 方法的核心处理流程。
        this.scanIngressService = scanIngressService;
        this.barcodeParser = barcodeParser;
    }

    /// <summary>
    /// 上传一次扫描批次，支持单次提交一个或多个条码，并返回每个条码的受理结果与失败原因。
    /// </summary>
    /// <param name="request">扫描上传请求，包含条码列表、设备编码、扫描时间与测量数据。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>逐条码的扫描受理结果。</returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ScanUploadResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ScanUploadResponse>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ScanUploadResponse>>>> UploadAsync([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ScanUploadRequest? request, CancellationToken cancellationToken) {
        // 步骤：执行 UploadAsync 方法的核心处理流程。
        if (request is null) {
            return BadRequest(ApiResponse<IReadOnlyList<ScanUploadResponse>>.Fail(EmptyRequestBodyMessage));
        }

        if (!TryBuildBarcodes(request, out var barcodes, out var barcodeValidationMessage)) {
            return BadRequest(ApiResponse<IReadOnlyList<ScanUploadResponse>>.Fail(barcodeValidationMessage));
        }

        if (barcodes.Count > 1 && !TryValidateBarcodeBatchChuteConsistency(barcodes, out var batchValidationMessage)) {
            return BadRequest(ApiResponse<IReadOnlyList<ScanUploadResponse>>.Fail(batchValidationMessage));
        }

        if (string.IsNullOrWhiteSpace(request.DeviceCode)) {
            return BadRequest(ApiResponse<IReadOnlyList<ScanUploadResponse>>.Fail("设备编码不能为空。"));
        }

        if (!LocalDateTimeNormalizer.TryNormalize(request.ScanTimeLocal, "扫描时间必须为本地时间，禁止传入 UTC 时间。", out var normalizedScanTime, out var validationMessage)) {
            return BadRequest(ApiResponse<IReadOnlyList<ScanUploadResponse>>.Fail(validationMessage));
        }

        var normalizedDeviceCode = request.DeviceCode.Trim();
        var normalizedTraceId = string.IsNullOrWhiteSpace(request.TraceId) ? string.Empty : request.TraceId.Trim();
        var responses = new List<ScanUploadResponse>(barcodes.Count);
        var hasRejected = false;
        var firstFailureMessage = string.Empty;
        for (var i = 0; i < barcodes.Count; i++) {
            var isPrimaryBarcode = i == 0;
            var applicationResult = await scanIngressService.ExecuteAsync(new ScanUploadApplicationRequest {
                Barcode = barcodes[i],
                DeviceCode = normalizedDeviceCode,
                ScanTimeLocal = normalizedScanTime,
                TraceId = normalizedTraceId,
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
    /// 执行 TryBuildBarcodes 方法。
    /// </summary>
    private static bool TryBuildBarcodes(ScanUploadRequest request, out List<string> normalizedBarcodes, out string validationMessage) {
        // 步骤：执行 TryBuildBarcodes 方法的核心处理流程。
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

    /// <summary>
    /// 执行 TryValidateBarcodeBatchChuteConsistency 方法。
    /// </summary>
    private bool TryValidateBarcodeBatchChuteConsistency(IReadOnlyList<string> barcodes, out string validationMessage) {
        // 步骤：执行 TryValidateBarcodeBatchChuteConsistency 方法的核心处理流程。
        validationMessage = string.Empty;
        string? firstChuteCode = null;
        foreach (var barcode in barcodes) {
            var parseResult = barcodeParser.Parse(barcode);
            if (!parseResult.IsValid || string.IsNullOrWhiteSpace(parseResult.TargetChuteCode)) {
                validationMessage = UnresolvableChuteMessage;
                return false;
            }

            if (firstChuteCode is null) {
                firstChuteCode = parseResult.TargetChuteCode;
                continue;
            }

            if (!string.Equals(firstChuteCode, parseResult.TargetChuteCode, StringComparison.Ordinal)) {
                validationMessage = MultipleChutesMessage;
                return false;
            }
        }

        return true;
    }
}

