using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.DropLogAggregate;
using EverydayChain.Hub.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 定义 DropFeedbackService 类型。
/// </summary>
public sealed class DropFeedbackService : IDropFeedbackService {
    /// <summary>
    /// 存储 _businessTaskRepository 字段。
    /// </summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;

    /// <summary>
    /// 存储 _dropLogRepository 字段。
    /// </summary>
    private readonly IDropLogRepository _dropLogRepository;

    /// <summary>
    /// 存储 _logger 字段。
    /// </summary>
    private readonly ILogger<DropFeedbackService> _logger;

    /// <summary>
    /// 执行 DropFeedbackService 方法。
    /// </summary>
    public DropFeedbackService(
        IBusinessTaskRepository businessTaskRepository,
        IDropLogRepository dropLogRepository,
        ILogger<DropFeedbackService> logger) {
            // 步骤：执行 DropFeedbackService 方法的核心处理流程。
        _businessTaskRepository = businessTaskRepository;
        _dropLogRepository = dropLogRepository;
        _logger = logger;
    }

    /// <summary>
    /// 执行 ExecuteAsync 方法。
    /// </summary>
    public async Task<DropFeedbackApplicationResult> ExecuteAsync(DropFeedbackApplicationRequest request, CancellationToken cancellationToken) {
        // 步骤：执行 ExecuteAsync 方法的核心处理流程。
        var normalizedTaskCode = string.IsNullOrWhiteSpace(request.TaskCode) ? null : request.TaskCode.Trim();
        var normalizedBarcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim();
        var normalizedActualChuteCode = string.IsNullOrWhiteSpace(request.ActualChuteCode) ? null : request.ActualChuteCode.Trim();
        var normalizedFailureReason = string.IsNullOrWhiteSpace(request.FailureReason) ? null : request.FailureReason.Trim();

        var hasTaskCode = normalizedTaskCode != null;
        var hasBarcode = normalizedBarcode != null;

        if (!hasTaskCode && !hasBarcode) {
            return new DropFeedbackApplicationResult {
                IsAccepted = false,
                FailureReason = "任务编码与条码不能同时为空，至少提供一项。",
                Message = "参数校验失败。"
            };
        }

        var task = hasTaskCode
            ? await _businessTaskRepository.FindByTaskCodeAsync(normalizedTaskCode!, cancellationToken)
            : null;

        if (task == null && hasBarcode) {
            task = await _businessTaskRepository.FindByBarcodeAsync(normalizedBarcode!, cancellationToken);
        }

        if (task == null) {
            return new DropFeedbackApplicationResult {
                IsAccepted = false,
                FailureReason = "TaskNotFound",
                Message = $"未找到任务编码 [{normalizedTaskCode}] 或条码 [{normalizedBarcode}] 对应的业务任务。"
            };
        }

        if (hasTaskCode && hasBarcode
            && !string.IsNullOrWhiteSpace(task.Barcode)
            && !string.Equals(task.Barcode, normalizedBarcode, StringComparison.OrdinalIgnoreCase)) {
            return new DropFeedbackApplicationResult {
                IsAccepted = false,
                TaskCode = task.TaskCode,
                Status = task.Status.ToString(),
                FailureReason = "BarcodeMismatch",
                Message = $"提供的条码 [{normalizedBarcode}] 与任务 [{task.TaskCode}] 关联条码不一致。"
            };
        }

        if (!IsDropAllowedStatus(task.Status)) {
            return new DropFeedbackApplicationResult {
                IsAccepted = false,
                TaskCode = task.TaskCode,
                Status = task.Status.ToString(),
                FailureReason = "InvalidTaskStatus",
                Message = $"任务 [{task.TaskCode}] 当前状态 [{task.Status}] 不允许落格回传。"
            };
        }

        if (request.IsSuccess) {
            if (normalizedActualChuteCode is null) {
                return new DropFeedbackApplicationResult
                {
                    IsAccepted = false,
                    TaskCode = task.TaskCode,
                    Status = task.Status.ToString(),
                    FailureReason = "ActualChuteCodeRequired",
                    Message = $"任务 [{task.TaskCode}] 落格成功回传时 ActualChuteCode 不能为空白。"
                };
            }

            task.Status = BusinessTaskStatus.Dropped;
            task.FeedbackStatus = BusinessTaskFeedbackStatus.Pending;
            task.ActualChuteCode = normalizedActualChuteCode;
            task.DroppedAtLocal = request.DropTimeLocal;
            task.IsFeedbackReported = false;
            task.FeedbackTimeLocal = null;
        } else {
            task.Status = BusinessTaskStatus.Exception;
            task.FailureReason = normalizedFailureReason;
            task.IsException = true;
        }

        task.UpdatedTimeLocal = DateTime.Now;
        task.RefreshQueryFields();

        await _businessTaskRepository.UpdateAsync(task, cancellationToken);

        await WriteDropLogSilentlyAsync(
            businessTaskId: task.Id,
            taskCode: task.TaskCode,
            barcode: normalizedBarcode,
            actualChuteCode: normalizedActualChuteCode,
            isSuccess: request.IsSuccess,
            failureReason: request.IsSuccess ? null : normalizedFailureReason,
            dropTimeLocal: request.DropTimeLocal,
            ct: cancellationToken);

        return new DropFeedbackApplicationResult {
            IsAccepted = true,
            TaskCode = task.TaskCode,
            Status = task.Status.ToString(),
            Message = request.IsSuccess
                ? $"任务 [{task.TaskCode}] 落格成功，已推进到 {task.Status}。"
                : $"任务 [{task.TaskCode}] 落格异常，已推进到 {task.Status}。"
        };
    }

    private static bool IsDropAllowedStatus(BusinessTaskStatus status)
    {
        return status == BusinessTaskStatus.Scanned
            || status == BusinessTaskStatus.Dropped
            || status == BusinessTaskStatus.FeedbackPending;
    }

    /// <summary>
    /// 执行 WriteDropLogSilentlyAsync 方法。
    /// </summary>
    private async Task WriteDropLogSilentlyAsync(
        long businessTaskId,
        string taskCode,
        string? barcode,
        string? actualChuteCode,
        bool isSuccess,
        string? failureReason,
        DateTime? dropTimeLocal,
        CancellationToken ct)
    {
        // 步骤：执行 IsDropAllowedStatus 方法的核心处理流程。
        try
        {
            var log = new DropLogEntity
            {
                BusinessTaskId = businessTaskId,
                TaskCode = taskCode,
                Barcode = barcode,
                ActualChuteCode = actualChuteCode,
                IsSuccess = isSuccess,
                FailureReason = failureReason,
                DropTimeLocal = dropTimeLocal,
                CreatedTimeLocal = DateTime.Now
            };
            await _dropLogRepository.SaveAsync(log, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "落格日志写入失败，不影响主流程。TaskCode={TaskCode}", taskCode);
        }
    }
}

