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
        // 步骤 1：按任务编码或条码定位任务。
        var hasTaskCode = !string.IsNullOrWhiteSpace(request.TaskCode);
        var hasBarcode = !string.IsNullOrWhiteSpace(request.Barcode);

        if (!hasTaskCode && !hasBarcode) {
            return new DropFeedbackApplicationResult {
                IsAccepted = false,
                FailureReason = "任务编码与条码不能同时为空，至少提供一项。",
                Message = "参数校验失败。"
            };
        }

        var task = hasTaskCode
            ? await _businessTaskRepository.FindByTaskCodeAsync(request.TaskCode.Trim(), cancellationToken)
            : null;

        if (task == null && hasBarcode) {
            task = await _businessTaskRepository.FindByBarcodeAsync(request.Barcode.Trim(), cancellationToken);
        }

        if (task == null) {
            return new DropFeedbackApplicationResult {
                IsAccepted = false,
                FailureReason = "TaskNotFound",
                Message = $"未找到任务编码 [{request.TaskCode}] 或条码 [{request.Barcode}] 对应的业务任务。"
            };
        }

        // 步骤 2：同时提供 TaskCode 与 Barcode 时，校验 Barcode 一致性。
        if (hasTaskCode && hasBarcode
            && !string.IsNullOrWhiteSpace(task.Barcode)
            && !string.Equals(task.Barcode, request.Barcode.Trim(), StringComparison.OrdinalIgnoreCase)) {
            return new DropFeedbackApplicationResult {
                IsAccepted = false,
                TaskCode = task.TaskCode,
                Status = task.Status.ToString(),
                FailureReason = "BarcodeMismatch",
                Message = $"提供的条码 [{request.Barcode}] 与任务 [{task.TaskCode}] 关联条码不一致。"
            };
        }

        // 步骤 3：校验任务状态是否允许推进（仅已扫描任务可落格）。
        if (task.Status != BusinessTaskStatus.Scanned) {
            return new DropFeedbackApplicationResult {
                IsAccepted = false,
                TaskCode = task.TaskCode,
                Status = task.Status.ToString(),
                FailureReason = "InvalidTaskStatus",
                Message = $"任务 [{task.TaskCode}] 当前状态 [{task.Status}] 不允许落格回传，仅已扫描任务可推进。"
            };
        }

        // 步骤 4：推进状态机。
        if (request.IsSuccess) {
            task.Status = BusinessTaskStatus.Dropped;
            task.FeedbackStatus = BusinessTaskFeedbackStatus.Pending;
            task.ActualChuteCode = string.IsNullOrWhiteSpace(request.ActualChuteCode) ? task.ActualChuteCode : request.ActualChuteCode.Trim();
            task.DroppedAtLocal = request.DropTimeLocal;
            // 落格成功后任务进入待回传阶段，需清理旧回传成功标记并等待新的 WMS 回写结果。
            task.IsFeedbackReported = false;
            task.FeedbackTimeLocal = null;
        } else {
            task.Status = BusinessTaskStatus.Exception;
            task.FailureReason = (request.FailureReason ?? string.Empty).Trim();
            task.IsException = true;
        }

        task.UpdatedTimeLocal = DateTime.Now;

        // 步骤 5：持久化。
        await _businessTaskRepository.UpdateAsync(task, cancellationToken);

        // 步骤 6：写落格日志（成功与失败均记录，异常不影响主流程）。
        await WriteDropLogSilentlyAsync(
            businessTaskId: task.Id,
            taskCode: task.TaskCode,
            barcode: request.Barcode,
            actualChuteCode: request.ActualChuteCode,
            isSuccess: request.IsSuccess,
            failureReason: request.IsSuccess ? null : request.FailureReason,
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
                Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim(),
                ActualChuteCode = string.IsNullOrWhiteSpace(actualChuteCode) ? null : actualChuteCode.Trim(),
                IsSuccess = isSuccess,
                FailureReason = string.IsNullOrWhiteSpace(failureReason) ? null : failureReason.Trim(),
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
