using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 扫描上传应用服务实现，协调条码解析、任务匹配与状态推进链路。
/// </summary>
public sealed class ScanIngressService : IScanIngressService {
    /// <summary>
    /// 条码解析服务。
    /// </summary>
    private readonly IBarcodeParser _barcodeParser;

    /// <summary>
    /// 任务执行服务。
    /// </summary>
    private readonly ITaskExecutionService _taskExecutionService;

    /// <summary>
    /// 初始化扫描上传应用服务。
    /// </summary>
    /// <param name="barcodeParser">条码解析服务。</param>
    /// <param name="taskExecutionService">任务执行服务。</param>
    public ScanIngressService(IBarcodeParser barcodeParser, ITaskExecutionService taskExecutionService) {
        _barcodeParser = barcodeParser;
        _taskExecutionService = taskExecutionService;
    }

    /// <summary>
    /// 处理扫描上传请求：解析条码、匹配任务、推进状态并返回标准化结果。
    /// </summary>
    /// <param name="request">扫描上传请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>处理结果。</returns>
    public async Task<ScanUploadApplicationResult> ExecuteAsync(ScanUploadApplicationRequest request, CancellationToken cancellationToken) {
        // 步骤 1：解析条码。
        var parseResult = _barcodeParser.Parse(request.Barcode);
        if (!parseResult.IsValid) {
            return new ScanUploadApplicationResult {
                IsAccepted = false,
                TaskCode = string.Empty,
                BarcodeType = BarcodeType.Unknown.ToString(),
                FailureReason = ConvertFailureReasonToCode(parseResult.FailureReason),
                Message = parseResult.FailureMessage
            };
        }

        // 步骤 2：将解析结果写入应用请求并推进任务状态。
        var executionRequest = new ScanUploadApplicationRequest {
            Barcode = request.Barcode,
            DeviceCode = request.DeviceCode,
            ScanTimeLocal = request.ScanTimeLocal,
            TraceId = request.TraceId,
            LengthMm = request.LengthMm,
            WidthMm = request.WidthMm,
            HeightMm = request.HeightMm,
            VolumeMm3 = request.VolumeMm3,
            WeightGram = request.WeightGram,
            TargetChuteCode = parseResult.TargetChuteCode,
            BarcodeType = parseResult.BarcodeType.ToString()
        };
        var execResult = await _taskExecutionService.MarkScannedAsync(executionRequest, cancellationToken);
        if (!execResult.IsSuccess) {
            return new ScanUploadApplicationResult {
                IsAccepted = false,
                TaskCode = execResult.TaskCode,
                BarcodeType = parseResult.BarcodeType.ToString(),
                FailureReason = "TaskNotMatchedOrInvalidState",
                Message = execResult.FailureReason
            };
        }

        return new ScanUploadApplicationResult {
            IsAccepted = true,
            TaskCode = execResult.TaskCode,
            BarcodeType = parseResult.BarcodeType.ToString(),
            FailureReason = string.Empty,
            Message = $"扫描请求已受理，任务 [{execResult.TaskCode}] 状态已更新为 {execResult.TaskStatus}。"
        };
    }

    /// <summary>
    /// 将失败语义枚举转换为统一失败代码。
    /// </summary>
    /// <param name="failureReason">失败语义枚举。</param>
    /// <returns>失败代码字符串。</returns>
    private static string ConvertFailureReasonToCode(BarcodeParseFailureReason failureReason) {
        return failureReason switch {
            BarcodeParseFailureReason.InvalidBarcode => nameof(BarcodeParseFailureReason.InvalidBarcode),
            BarcodeParseFailureReason.UnsupportedBarcodeType => nameof(BarcodeParseFailureReason.UnsupportedBarcodeType),
            BarcodeParseFailureReason.ParseError => nameof(BarcodeParseFailureReason.ParseError),
            _ => string.Empty
        };
    }
}
