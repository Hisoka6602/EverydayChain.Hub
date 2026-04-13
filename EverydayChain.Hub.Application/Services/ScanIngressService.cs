using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 扫描上传应用服务骨架实现。
/// </summary>
public sealed class ScanIngressService : IScanIngressService {
    /// <summary>
    /// 条码解析服务。
    /// </summary>
    private readonly IBarcodeParser barcodeParser;

    /// <summary>
    /// 初始化扫描上传应用服务。
    /// </summary>
    /// <param name="barcodeParser">条码解析服务。</param>
    public ScanIngressService(IBarcodeParser barcodeParser) {
        this.barcodeParser = barcodeParser;
    }

    /// <summary>
    /// 处理扫描上传请求并返回标准化结果。
    /// </summary>
    /// <param name="request">扫描上传请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>处理结果。</returns>
    public Task<ScanUploadApplicationResult> ExecuteAsync(ScanUploadApplicationRequest request, CancellationToken cancellationToken) {
        _ = cancellationToken;
        var parseResult = barcodeParser.Parse(request.Barcode);
        if (!parseResult.IsValid) {
            return Task.FromResult(new ScanUploadApplicationResult {
                IsAccepted = false,
                TaskCode = string.Empty,
                BarcodeType = BarcodeType.Unknown.ToString(),
                FailureReason = ConvertFailureReasonToCode(parseResult.FailureReason),
                Message = parseResult.FailureMessage
            });
        }

        var taskCodeCandidate = string.IsNullOrWhiteSpace(request.DeviceCode)
            ? string.Empty
            : $"{request.DeviceCode}-{request.ScanTimeLocal:yyyyMMddHHmmss}";
        var normalizedTaskCode = string.IsNullOrWhiteSpace(taskCodeCandidate)
            ? $"TASK-{parseResult.NormalizedBarcode}"
            : taskCodeCandidate;
        var result = new ScanUploadApplicationResult {
            IsAccepted = true,
            TaskCode = normalizedTaskCode,
            BarcodeType = parseResult.BarcodeType.ToString(),
            FailureReason = string.Empty,
            Message = "扫描请求已受理，条码解析已完成，后续阶段将接入匹配与状态推进链路。"
        };
        return Task.FromResult(result);
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
