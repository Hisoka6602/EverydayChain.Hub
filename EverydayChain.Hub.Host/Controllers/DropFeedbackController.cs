using Microsoft.AspNetCore.Mvc;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 落格回传接口。
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
    /// </summary>
    /// <param name="request">落格回传请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>处理结果。</returns>
    [HttpPost("confirm")]
    [ProducesResponseType(typeof(ApiResponse<DropFeedbackResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DropFeedbackResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<DropFeedbackResponse>>> ConfirmAsync([FromBody] DropFeedbackRequest request, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(request.Barcode)) {
            return BadRequest(ApiResponse<DropFeedbackResponse>.Fail("条码不能为空。"));
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
            Barcode = request.Barcode.Trim(),
            ActualChuteCode = request.ActualChuteCode.Trim(),
            DropTimeLocal = normalizedDropTime
        }, cancellationToken);

        var response = new DropFeedbackResponse {
            IsAccepted = applicationResult.IsAccepted,
            TaskCode = applicationResult.TaskCode,
            Status = applicationResult.Status
        };

        return Ok(ApiResponse<DropFeedbackResponse>.Success(response, applicationResult.Message));
    }
}
