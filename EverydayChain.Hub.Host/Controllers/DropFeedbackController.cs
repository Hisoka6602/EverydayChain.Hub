using Microsoft.AspNetCore.Mvc;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 提供落格回传接口，用于回写条码或任务在设备端完成落格后的最终结果。
/// </summary>
[ApiController]
[Route("api/v1/drop-feedback")]
public sealed class DropFeedbackController : ControllerBase {
    /// <summary>
    /// 存储 dropFeedbackService 字段。
    /// </summary>
    private readonly IDropFeedbackService dropFeedbackService;

    /// <summary>
    /// 执行 DropFeedbackController 方法。
    /// </summary>
    public DropFeedbackController(IDropFeedbackService dropFeedbackService) {
        // 步骤：执行 DropFeedbackController 方法的核心处理流程。
        this.dropFeedbackService = dropFeedbackService;
    }

    /// <summary>
    /// 确认一次落格结果，将实际落格编码、落格时间和成功失败状态写回业务任务。
    /// </summary>
    /// <param name="request">落格回传请求，支持通过任务编码或条码定位任务。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>落格回传处理结果。</returns>
    [HttpPost("confirm")]
    [ProducesResponseType(typeof(ApiResponse<DropFeedbackResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DropFeedbackResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<DropFeedbackResponse>>> ConfirmAsync([FromBody] DropFeedbackRequest request, CancellationToken cancellationToken) {
        // 步骤：执行 ConfirmAsync 方法的核心处理流程。
        if (string.IsNullOrWhiteSpace(request.Barcode) && string.IsNullOrWhiteSpace(request.TaskCode)) {
            return BadRequest(ApiResponse<DropFeedbackResponse>.Fail("任务编码与条码不能同时为空，至少提供一项。"));
        }

        if (string.IsNullOrWhiteSpace(request.ActualChuteCode)) {
            return BadRequest(ApiResponse<DropFeedbackResponse>.Fail("实际落格编码不能为空。"));
        }

        if (!LocalDateTimeNormalizer.TryNormalize(request.DropTimeLocal, "落格时间必须为本地时间，禁止传入 UTC 时间。", out var normalizedDropTime, out var validationMessage)) {
            return BadRequest(ApiResponse<DropFeedbackResponse>.Fail(validationMessage));
        }

        var normalizedTaskCode = TaskCodeNormalizer.NormalizeOrEmpty(request.TaskCode);
        var applicationResult = await dropFeedbackService.ExecuteAsync(new DropFeedbackApplicationRequest {
            TaskCode = normalizedTaskCode,
            Barcode = (request.Barcode ?? string.Empty).Trim(),
            ActualChuteCode = request.ActualChuteCode.Trim(),
            DropTimeLocal = normalizedDropTime,
            IsSuccess = request.IsSuccess,
            FailureReason = request.FailureReason
        }, cancellationToken);

        var response = new DropFeedbackResponse {
            IsAccepted = applicationResult.IsAccepted,
            TaskCode = applicationResult.TaskCode,
            Status = applicationResult.Status
        };

        return Ok(ApiResponse<DropFeedbackResponse>.Success(response, applicationResult.Message));
    }
}

