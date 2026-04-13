using Microsoft.AspNetCore.Mvc;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 请求格口接口。
/// </summary>
[ApiController]
[Route("api/v1/chute")]
public sealed class ChuteController : ControllerBase {
    /// <summary>
    /// 格口查询应用服务。
    /// </summary>
    private readonly IChuteQueryService chuteQueryService;

    /// <summary>
    /// 初始化请求格口控制器。
    /// </summary>
    /// <param name="chuteQueryService">格口查询应用服务。</param>
    public ChuteController(IChuteQueryService chuteQueryService) {
        this.chuteQueryService = chuteQueryService;
    }

    /// <summary>
    /// 查询目标格口。
    /// </summary>
    /// <param name="request">请求格口入参。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>格口结果。</returns>
    [HttpPost("resolve")]
    [ProducesResponseType(typeof(ApiResponse<ChuteResolveResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ChuteResolveResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ChuteResolveResponse>>> ResolveAsync([FromBody] ChuteResolveRequest request, CancellationToken cancellationToken) {
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
