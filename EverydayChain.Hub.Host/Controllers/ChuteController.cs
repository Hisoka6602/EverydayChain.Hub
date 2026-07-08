using Microsoft.AspNetCore.Mvc;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 提供条码到目标格口的解析接口，用于在扫描前快速确认条码应落入的格口。
/// </summary>
[ApiController]
[Route("api/v1/chute")]
public sealed class ChuteController : ControllerBase {
    /// <summary>
    /// 存储 chuteQueryService 字段。
    /// </summary>
    private readonly IChuteQueryService chuteQueryService;

    /// <summary>
    /// 执行 ChuteController 方法。
    /// </summary>
    public ChuteController(IChuteQueryService chuteQueryService) {
        // 步骤：执行 ChuteController 方法的核心处理流程。
        this.chuteQueryService = chuteQueryService;
    }

    /// <summary>
    /// 根据任务编码和条码解析目标格口，返回是否解析成功以及命中的格口编码。
    /// </summary>
    /// <param name="request">格口解析请求，至少包含条码，可选传入任务编码辅助定位。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>格口解析结果。</returns>
    [HttpPost("resolve")]
    [ProducesResponseType(typeof(ApiResponse<ChuteResolveResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ChuteResolveResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ChuteResolveResponse>>> ResolveAsync([FromBody] ChuteResolveRequest request, CancellationToken cancellationToken) {
        // 步骤：执行 ResolveAsync 方法的核心处理流程。
        if (string.IsNullOrWhiteSpace(request.Barcode)) {
            return BadRequest(ApiResponse<ChuteResolveResponse>.Fail("条码不能为空。"));
        }

        var normalizedTaskCode = TaskCodeNormalizer.NormalizeOrEmpty(request.TaskCode);
        var applicationResult = await chuteQueryService.ExecuteAsync(new ChuteResolveApplicationRequest {
            TaskCode = normalizedTaskCode,
            Barcode = request.Barcode.Trim()
        }, cancellationToken);

        var response = new ChuteResolveResponse {
            IsResolved = applicationResult.IsResolved,
            TaskCode = applicationResult.TaskCode,
            ChuteCode = applicationResult.ChuteCode
        };

        return Ok(ApiResponse<ChuteResolveResponse>.Success(response, applicationResult.Message));
    }
}

