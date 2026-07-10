using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Utilities;
using EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;
using EverydayChain.Hub.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 定义 ScanIngressService 类型。
/// </summary>
public sealed class ScanIngressService : IScanIngressService {
    /// <summary>
    /// 存储 _barcodeParser 字段。
    /// </summary>
    private readonly IBarcodeParser _barcodeParser;

    /// <summary>
    /// 存储 _taskExecutionService 字段。
    /// </summary>
    private readonly ITaskExecutionService _taskExecutionService;
    /// <summary>
    /// 存储 _scanLogRepository 字段。
    /// </summary>
    private readonly IScanLogRepository _scanLogRepository;
    /// <summary>
    /// 存储 _logger 字段。
    /// </summary>
    private readonly ILogger<ScanIngressService> _logger;

    /// <summary>
    /// 执行 ScanIngressService 方法。
    /// </summary>
    public ScanIngressService(
        IBarcodeParser barcodeParser,
        ITaskExecutionService taskExecutionService,
        IScanLogRepository scanLogRepository,
        ILogger<ScanIngressService> logger) {
            // 步骤：执行 ScanIngressService 方法的核心处理流程。
        _barcodeParser = barcodeParser;
        _taskExecutionService = taskExecutionService;
        _scanLogRepository = scanLogRepository;
        _logger = logger;
    }

    /// <summary>
    /// 执行 ExecuteAsync 方法。
    /// </summary>
    public async Task<ScanUploadApplicationResult> ExecuteAsync(ScanUploadApplicationRequest request, CancellationToken cancellationToken) {
        // 步骤：执行 ExecuteAsync 方法的核心处理流程。
        var parseResult = _barcodeParser.Parse(request.Barcode);
        var barcodeType = ChineseDisplayText.ForBarcodeType(parseResult.BarcodeType);
        if (!parseResult.IsValid) {
            await WriteScanLogSilentlyAsync(
                request.Barcode,
                request.DeviceCode,
                false,
                string.IsNullOrWhiteSpace(parseResult.FailureMessage)
                    ? ConvertFailureReasonToCode(parseResult.FailureReason)
                    : parseResult.FailureMessage,
                request.TraceId,
                request.ScanTimeLocal,
                cancellationToken);

            return new ScanUploadApplicationResult {
                IsAccepted = false,
                TaskCode = string.Empty,
                BarcodeType = barcodeType,
                FailureReason = ConvertFailureReasonToCode(parseResult.FailureReason),
                Message = parseResult.FailureMessage
            };
        }

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
            BarcodeType = barcodeType
        };
        var execResult = await _taskExecutionService.MarkScannedAsync(executionRequest, cancellationToken);
        if (!execResult.IsSuccess) {
            return new ScanUploadApplicationResult {
                IsAccepted = false,
                TaskCode = execResult.TaskCode,
                BarcodeType = barcodeType,
                FailureReason = "任务未匹配或状态不允许流转",
                Message = execResult.FailureReason
            };
        }

        return new ScanUploadApplicationResult {
            IsAccepted = true,
            TaskCode = execResult.TaskCode,
            BarcodeType = barcodeType,
            FailureReason = string.Empty,
            Message = $"扫描请求已受理，任务 [{execResult.TaskCode}] 状态已更新为 {execResult.TaskStatus}。"
        };
    }

    /// <summary>
    /// 执行 ConvertFailureReasonToCode 方法。
    /// </summary>
    private static string ConvertFailureReasonToCode(BarcodeParseFailureReason failureReason) {
        // 步骤：执行 ConvertFailureReasonToCode 方法的核心处理流程。
        return failureReason switch {
            BarcodeParseFailureReason.InvalidBarcode => ChineseDisplayText.ForBarcodeParseFailureReason(BarcodeParseFailureReason.InvalidBarcode),
            BarcodeParseFailureReason.UnsupportedBarcodeType => ChineseDisplayText.ForBarcodeParseFailureReason(BarcodeParseFailureReason.UnsupportedBarcodeType),
            BarcodeParseFailureReason.ParseError => ChineseDisplayText.ForBarcodeParseFailureReason(BarcodeParseFailureReason.ParseError),
            _ => string.Empty
        };
    }

    /// <summary>
    /// 执行 WriteScanLogSilentlyAsync 方法。
    /// </summary>
    private async Task WriteScanLogSilentlyAsync(
        string barcode,
        string? deviceCode,
        bool isMatched,
        string? failureReason,
        string? traceId,
        DateTime scanTimeLocal,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 nameof 方法的核心处理流程。
        try
        {
            await _scanLogRepository.SaveAsync(new ScanLogEntity
            {
                Barcode = string.IsNullOrWhiteSpace(barcode) ? string.Empty : barcode.Trim(),
                DeviceCode = string.IsNullOrWhiteSpace(deviceCode) ? null : deviceCode.Trim(),
                IsMatched = isMatched,
                FailureReason = failureReason,
                TraceId = string.IsNullOrWhiteSpace(traceId) ? null : traceId.Trim(),
                ScanTimeLocal = scanTimeLocal,
                CreatedTimeLocal = DateTime.Now
            }, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scan parse failure log write failed. BarcodeLength={BarcodeLength}", barcode?.Length ?? 0);
        }
    }
}

