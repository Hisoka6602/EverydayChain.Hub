using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;
using EverydayChain.Hub.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.TaskExecution.Services;

/// <summary>
/// 任务执行服务实现，负责推进业务任务状态并持久化。
/// </summary>
public sealed class TaskExecutionService : ITaskExecutionService
{
    /// <summary>
    /// 扫描匹配服务。
    /// </summary>
    private readonly IScanMatchService _scanMatchService;

    /// <summary>
    /// 业务任务仓储。
    /// </summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;

    /// <summary>
    /// 扫描日志仓储。
    /// </summary>
    private readonly IScanLogRepository _scanLogRepository;

    /// <summary>
    /// 日志记录器。
    /// </summary>
    private readonly ILogger<TaskExecutionService> _logger;

    /// <summary>
    /// 初始化任务执行服务。
    /// </summary>
    /// <param name="scanMatchService">扫描匹配服务。</param>
    /// <param name="businessTaskRepository">业务任务仓储。</param>
    /// <param name="scanLogRepository">扫描日志仓储。</param>
    /// <param name="logger">日志记录器。</param>
    public TaskExecutionService(
        IScanMatchService scanMatchService,
        IBusinessTaskRepository businessTaskRepository,
        IScanLogRepository scanLogRepository,
        ILogger<TaskExecutionService> logger)
    {
        _scanMatchService = scanMatchService;
        _businessTaskRepository = businessTaskRepository;
        _scanLogRepository = scanLogRepository;
        _logger = logger;
    }

    /// <summary>
    /// 将业务任务标记为已扫描，推进状态为 <see cref="BusinessTaskStatus.Scanned"/>。
    /// 步骤：1. 按条码匹配任务；2. 检查当前状态是否允许推进；3. 更新状态与扫描信息；4. 持久化；5. 写扫描日志。
    /// </summary>
    /// <param name="request">扫描上传请求，包含条码、设备编码等信息。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>任务执行结果。</returns>
    public async Task<TaskExecutionResult> MarkScannedAsync(ScanUploadApplicationRequest request, CancellationToken ct)
    {
        var normalizedBarcode = string.IsNullOrWhiteSpace(request.Barcode) ? string.Empty : request.Barcode.Trim();
        var normalizedDeviceCode = string.IsNullOrWhiteSpace(request.DeviceCode) ? null : request.DeviceCode.Trim();
        var normalizedTraceId = string.IsNullOrWhiteSpace(request.TraceId) ? null : request.TraceId.Trim();

        // 步骤 1：按条码匹配任务。
        var matchResult = await _scanMatchService.MatchByBarcodeAsync(normalizedBarcode, ct);
        if (!matchResult.IsMatched || matchResult.Task == null)
        {
            // 匹配失败：写扫描失败日志后返回。
            await WriteScanLogSilentlyAsync(
                businessTaskId: null,
                taskCode: null,
                barcode: normalizedBarcode,
                deviceCode: normalizedDeviceCode,
                isMatched: false,
                failureReason: matchResult.FailureReason,
                traceId: normalizedTraceId,
                scanTimeLocal: request.ScanTimeLocal,
                ct: ct);

            return new TaskExecutionResult
            {
                IsSuccess = false,
                FailureReason = matchResult.FailureReason
            };
        }

        var task = matchResult.Task;

        // 步骤 2：检查当前状态是否允许推进到已扫描。
        if (task.Status != BusinessTaskStatus.Created && task.Status != BusinessTaskStatus.Scanned)
        {
            var reason = $"任务当前状态 [{task.Status}] 不允许推进到已扫描。";
            var now = DateTime.Now;

            // 状态校验失败时递增扫描重试次数，为回流规则提供判定依据。
            task.ScanRetryCount++;
            task.UpdatedTimeLocal = now;
            await _businessTaskRepository.UpdateAsync(task, ct);

            await WriteScanLogSilentlyAsync(
                businessTaskId: task.Id,
                taskCode: task.TaskCode,
                barcode: request.Barcode,
                deviceCode: request.DeviceCode,
                isMatched: false,
                failureReason: reason,
                traceId: request.TraceId,
                scanTimeLocal: request.ScanTimeLocal,
                ct: ct);

            return new TaskExecutionResult
            {
                IsSuccess = false,
                TaskId = task.Id,
                TaskCode = task.TaskCode,
                TaskStatus = task.Status.ToString(),
                FailureReason = reason
            };
        }

        // 步骤 3：更新状态与扫描信息。
        task.Status = BusinessTaskStatus.Scanned;
        task.ScannedAtLocal = request.ScanTimeLocal;
        task.DeviceCode = normalizedDeviceCode ?? task.DeviceCode;
        task.TraceId = normalizedTraceId ?? task.TraceId;
        task.Barcode = string.IsNullOrWhiteSpace(task.Barcode) ? normalizedBarcode : task.Barcode;
        // 扫描维度字段采用“请求优先、缺省保留旧值”策略，避免空值覆盖历史有效测量数据。
        task.LengthMm = request.LengthMm ?? task.LengthMm;
        task.WidthMm = request.WidthMm ?? task.WidthMm;
        task.HeightMm = request.HeightMm ?? task.HeightMm;
        task.VolumeMm3 = request.VolumeMm3 ?? task.VolumeMm3;
        task.WeightGram = request.WeightGram ?? task.WeightGram;
        task.ScanCount++;
        task.UpdatedTimeLocal = DateTime.Now;

        // 步骤 4：持久化。
        await _businessTaskRepository.UpdateAsync(task, ct);

        // 步骤 5：写扫描成功日志。
        await WriteScanLogSilentlyAsync(
            businessTaskId: task.Id,
            taskCode: task.TaskCode,
            barcode: normalizedBarcode,
            deviceCode: normalizedDeviceCode,
            isMatched: true,
            failureReason: null,
            traceId: normalizedTraceId,
            scanTimeLocal: request.ScanTimeLocal,
            ct: ct);

        return new TaskExecutionResult
        {
            IsSuccess = true,
            TaskId = task.Id,
            TaskCode = task.TaskCode,
            TaskStatus = task.Status.ToString()
        };
    }

    /// <summary>
    /// 静默写入扫描日志，异常时仅记录日志不影响主流程。
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
        try
        {
            var log = new ScanLogEntity
            {
                BusinessTaskId = businessTaskId,
                TaskCode = taskCode,
                Barcode = barcode.Trim(),
                DeviceCode = string.IsNullOrWhiteSpace(deviceCode) ? null : deviceCode.Trim(),
                IsMatched = isMatched,
                FailureReason = string.IsNullOrWhiteSpace(failureReason) ? null : failureReason.Trim(),
                TraceId = string.IsNullOrWhiteSpace(traceId) ? null : traceId.Trim(),
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
            _logger.LogError(ex, "扫描日志写入失败，不影响主流程。BarcodeLength={BarcodeLength}", barcode?.Length ?? 0);
        }
    }
}
