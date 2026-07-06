using Microsoft.AspNetCore.Mvc;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 定义当前类型。
/// </summary>
[ApiController]
[Route("api/v1/chute")]
public sealed class ChuteController : ControllerBase {
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly IChuteQueryService chuteQueryService;

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public ChuteController(IChuteQueryService chuteQueryService) {
        // 步骤：按既定流程执行当前方法逻辑。
        this.chuteQueryService = chuteQueryService;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [HttpPost("resolve")]
    [ProducesResponseType(typeof(ApiResponse<ChuteResolveResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ChuteResolveResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ChuteResolveResponse>>> ResolveAsync([FromBody] ChuteResolveRequest request, CancellationToken cancellationToken) {
        // 步骤：按既定流程执行当前方法逻辑。
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

