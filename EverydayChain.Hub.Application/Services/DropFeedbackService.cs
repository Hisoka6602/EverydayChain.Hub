using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.DropLogAggregate;
using EverydayChain.Hub.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 落格回传应用服务实现，按任务编码或条码定位任务并推进落格状态。
/// </summary>
public sealed class DropFeedbackService : IDropFeedbackService {
    /// <summary>
    /// 业务任务仓储。
    /// </summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;

    /// <summary>
    /// 落格日志仓储。
    /// </summary>
    private readonly IDropLogRepository _dropLogRepository;

    /// <summary>
    /// 日志记录器。
    /// </summary>
    private readonly ILogger<DropFeedbackService> _logger;

    /// <summary>
    /// 初始化落格回传应用服务。
    /// </summary>
    /// <param name="businessTaskRepository">业务任务仓储。</param>
    /// <param name="dropLogRepository">落格日志仓储。</param>
    /// <param name="logger">日志记录器。</param>
    public DropFeedbackService(
        IBusinessTaskRepository businessTaskRepository,
        IDropLogRepository dropLogRepository,
        ILogger<DropFeedbackService> logger) {
        _businessTaskRepository = businessTaskRepository;
        _dropLogRepository = dropLogRepository;
        _logger = logger;
    }

    /// <summary>
    /// 处理落格回传：定位任务、校验状态、推进状态机并持久化。
    /// 步骤：1. 按任务编码或条码定位任务；2. 校验参数一致性；3. 推进状态；4. 持久化；5. 写落格日志；6. 返回结果。
    /// </summary>
    /// <param name="request">落格回传请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>处理结果。</returns>
    public async Task<DropFeedbackApplicationResult> ExecuteAsync(DropFeedbackApplicationRequest request, CancellationToken cancellationToken) {
        var normalizedTaskCode = string.IsNullOrWhiteSpace(request.TaskCode) ? null : request.TaskCode.Trim();
        var normalizedBarcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim();
        var normalizedActualChuteCode = string.IsNullOrWhiteSpace(request.ActualChuteCode) ? null : request.ActualChuteCode.Trim();
        var normalizedFailureReason = string.IsNullOrWhiteSpace(request.FailureReason) ? null : request.FailureReason.Trim();

        // 步骤 1：按任务编码或条码定位任务。
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

        // 步骤 2：同时提供 TaskCode 与 Barcode 时，校验 Barcode 一致性。
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

        // 步骤 3：校验任务状态是否允许推进（已扫描/已落格/待回传任务可重复落格覆盖）。
        if (!IsDropAllowedStatus(task.Status)) {
            return new DropFeedbackApplicationResult {
                IsAccepted = false,
                TaskCode = task.TaskCode,
                Status = task.Status.ToString(),
                FailureReason = "InvalidTaskStatus",
                Message = $"任务 [{task.TaskCode}] 当前状态 [{task.Status}] 不允许落格回传。"
            };
        }

        // 步骤 4：推进状态机。
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
            // 落格成功后任务进入待回传阶段，需清理旧回传成功标记并等待新的 WMS 回写结果。
            task.IsFeedbackReported = false;
            task.FeedbackTimeLocal = null;
        } else {
            task.Status = BusinessTaskStatus.Exception;
            task.FailureReason = normalizedFailureReason;
            task.IsException = true;
        }

        task.UpdatedTimeLocal = DateTime.Now;
        // 落格回传可能重复触发，必须基于本次 ActualChuteCode 重新计算 ResolvedDockCode，避免历史值残留。
        task.RefreshQueryFields();

        // 步骤 5：持久化。
        await _businessTaskRepository.UpdateAsync(task, cancellationToken);

        // 步骤 6：写落格日志（成功与失败均记录，异常不影响主流程）。
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

    /// <summary>
    /// 判断当前任务状态是否允许落格回传。
    /// </summary>
    /// <param name="status">任务状态。</param>
    /// <returns>允许返回 true，否则返回 false。</returns>
    private static bool IsDropAllowedStatus(BusinessTaskStatus status)
    {
        return status == BusinessTaskStatus.Scanned
            || status == BusinessTaskStatus.Dropped
            || status == BusinessTaskStatus.FeedbackPending;
    }

    /// <summary>
    /// 静默写入落格日志，异常时仅记录日志不影响主流程。
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
