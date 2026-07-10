using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Utilities;
using EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;
using EverydayChain.Hub.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.TaskExecution.Services;

/// <summary>
/// 定义 TaskExecutionService 类型。
/// </summary>
public sealed class TaskExecutionService : ITaskExecutionService
{
    /// <summary>
    /// 存储 _scanMatchService 字段。
    /// </summary>
    private readonly IScanMatchService _scanMatchService;
    /// <summary>
    /// 存储 _businessTaskRepository 字段。
    /// </summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;
    /// <summary>
    /// 存储 _scanLogRepository 字段。
    /// </summary>
    private readonly IScanLogRepository _scanLogRepository;
    /// <summary>
    /// 存储 _logger 字段。
    /// </summary>
    private readonly ILogger<TaskExecutionService> _logger;

    /// <summary>
    /// 执行 TaskExecutionService 方法。
    /// </summary>
    public TaskExecutionService(
        IScanMatchService scanMatchService,
        IBusinessTaskRepository businessTaskRepository,
        IScanLogRepository scanLogRepository,
        ILogger<TaskExecutionService> logger)
    {
        // 步骤：执行 TaskExecutionService 方法的核心处理流程。
        _scanMatchService = scanMatchService;
        _businessTaskRepository = businessTaskRepository;
        _scanLogRepository = scanLogRepository;
        _logger = logger;
    }

    public async Task<TaskExecutionResult> MarkScannedAsync(ScanUploadApplicationRequest request, CancellationToken ct)
    {
        var normalizedBarcode = string.IsNullOrWhiteSpace(request.Barcode) ? string.Empty : request.Barcode.Trim();
        var normalizedDeviceCode = string.IsNullOrWhiteSpace(request.DeviceCode) ? null : request.DeviceCode.Trim();
        var normalizedTraceId = string.IsNullOrWhiteSpace(request.TraceId) ? null : request.TraceId.Trim();
        var normalizedTargetChuteCode = string.IsNullOrWhiteSpace(request.TargetChuteCode) ? null : request.TargetChuteCode.Trim();

        var matchResult = await _scanMatchService.MatchByBarcodeAsync(normalizedBarcode, ct);
        if (!matchResult.IsMatched || matchResult.Task is null)
        {
            await WriteScanLogSilentlyAsync(
                businessTaskId: null,
                taskCode: null,
                barcode: normalizedBarcode,
                deviceCode: normalizedDeviceCode,
                isMatched: false,
                failureReason: matchResult.FailureReason,
                traceId: normalizedTraceId,
                scanTimeLocal: request.ScanTimeLocal,
                ct);

            return new TaskExecutionResult
            {
                IsSuccess = false,
                FailureReason = matchResult.FailureReason
            };
        }

        var task = matchResult.Task;
        if (!IsAllowedScanTransitionSourceStatus(task.Status))
        {
            var nowLocal = DateTime.Now;
            await _businessTaskRepository.IncrementScanRetryAsync(task.Id, task.CreatedTimeLocal, nowLocal, ct);
            var reason = $"任务状态 [{ChineseDisplayText.ForTaskStatus(task.Status)}] 不允许扫描流转。";
            await WriteScanLogSilentlyAsync(
                businessTaskId: task.Id,
                taskCode: task.TaskCode,
                barcode: normalizedBarcode,
                deviceCode: normalizedDeviceCode,
                isMatched: true,
                failureReason: reason,
                traceId: normalizedTraceId,
                scanTimeLocal: request.ScanTimeLocal,
                ct);
            return new TaskExecutionResult
            {
                IsSuccess = false,
                TaskId = task.Id,
                TaskCode = task.TaskCode,
                TaskStatus = ChineseDisplayText.ForTaskStatus(task.Status),
                FailureReason = reason
            };
        }

        var now = DateTime.Now;
        var updated = await _businessTaskRepository.TryMarkScannedAsync(
            task.Id,
            task.CreatedTimeLocal,
            new BusinessTaskScanUpdateCommand
            {
                Barcode = normalizedBarcode,
                DeviceCode = normalizedDeviceCode,
                TraceId = normalizedTraceId,
                TargetChuteCode = normalizedTargetChuteCode,
                ScanTimeLocal = request.ScanTimeLocal,
                UpdatedTimeLocal = now,
                LengthMm = request.LengthMm,
                WidthMm = request.WidthMm,
                HeightMm = request.HeightMm,
                VolumeMm3 = request.VolumeMm3,
                WeightGram = request.WeightGram
            },
            ct);

        if (!updated)
        {
            var currentTask = await _businessTaskRepository.FindByIdAsync(task.Id, ct);
            if (currentTask is not null)
            {
                await _businessTaskRepository.IncrementScanRetryAsync(currentTask.Id, currentTask.CreatedTimeLocal, now, ct);
            }

            var currentStatus = ChineseDisplayText.ForTaskStatus(currentTask?.Status ?? task.Status);
            var reason = $"任务状态 [{currentStatus}] 不允许扫描流转。";
            await WriteScanLogSilentlyAsync(
                businessTaskId: task.Id,
                taskCode: task.TaskCode,
                barcode: normalizedBarcode,
                deviceCode: normalizedDeviceCode,
                isMatched: true,
                failureReason: reason,
                traceId: normalizedTraceId,
                scanTimeLocal: request.ScanTimeLocal,
                ct);
            return new TaskExecutionResult
            {
                IsSuccess = false,
                TaskId = task.Id,
                TaskCode = task.TaskCode,
                TaskStatus = currentStatus,
                FailureReason = reason
            };
        }

        await WriteScanLogSilentlyAsync(
            businessTaskId: task.Id,
            taskCode: task.TaskCode,
            barcode: normalizedBarcode,
            deviceCode: normalizedDeviceCode,
            isMatched: true,
            failureReason: null,
            traceId: normalizedTraceId,
            scanTimeLocal: request.ScanTimeLocal,
            ct);

        return new TaskExecutionResult
        {
            IsSuccess = true,
            TaskId = task.Id,
            TaskCode = task.TaskCode,
            TaskStatus = ChineseDisplayText.ForTaskStatus(BusinessTaskStatus.Scanned)
        };
    }

    private static bool IsAllowedScanTransitionSourceStatus(BusinessTaskStatus status)
    {
        return status is BusinessTaskStatus.Created
            or BusinessTaskStatus.Scanned
            or BusinessTaskStatus.Dropped;
    }

    /// <summary>
    /// 执行 WriteScanLogSilentlyAsync 方法。
    /// </summary>
    private async Task WriteScanLogSilentlyAsync(
        long? businessTaskId,
        string? taskCode,
        string barcode,
        string? deviceCode,
        bool isMatched,
        string? failureReason,
        string? traceId,
        DateTime scanTimeLocal,
        CancellationToken ct)
    {
        // 步骤：执行 IsAllowedScanTransitionSourceStatus 方法的核心处理流程。
        try
        {
            var log = new ScanLogEntity
            {
                BusinessTaskId = businessTaskId,
                TaskCode = taskCode,
                Barcode = barcode,
                DeviceCode = deviceCode,
                IsMatched = isMatched,
                FailureReason = failureReason,
                TraceId = traceId,
                ScanTimeLocal = scanTimeLocal,
                CreatedTimeLocal = DateTime.Now
            };
            await _scanLogRepository.SaveAsync(log, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scan log write failed without affecting the main scan flow. BarcodeLength={BarcodeLength}", barcode?.Length ?? 0);
        }
    }
}

