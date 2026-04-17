using Microsoft.AspNetCore.Mvc;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 落格回传控制器，负责接收分拣落格结果并回写业务任务状态。
/// </summary>
[ApiController]
[Route("api/v1/drop-feedback")]
public sealed class DropFeedbackController : ControllerBase {
    /// <summary>
    /// 落格回传应用服务。
    /// </summary>
    private readonly IDropFeedbackService dropFeedbackService;

    /// <summary>
    /// 初始化落格回传控制器。
    /// </summary>
    /// <param name="dropFeedbackService">落格回传应用服务。</param>
    public DropFeedbackController(IDropFeedbackService dropFeedbackService) {
        this.dropFeedbackService = dropFeedbackService;
    }

    /// <summary>
    /// 接收落格回传请求。
    /// 请求条件：任务编码与条码至少一项，实际落格编码必填，落格时间必须为本地时间。
    /// 返回语义：受理成功返回 200；关键字段缺失或时间非法返回 400。
    /// </summary>
    /// <param name="request">落格回传请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>落格回传处理结果，包含受理状态、任务编码与最新任务状态。</returns>
    [HttpPost("confirm")]
    [ProducesResponseType(typeof(ApiResponse<DropFeedbackResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DropFeedbackResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<DropFeedbackResponse>>> ConfirmAsync([FromBody] DropFeedbackRequest request, CancellationToken cancellationToken) {
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
