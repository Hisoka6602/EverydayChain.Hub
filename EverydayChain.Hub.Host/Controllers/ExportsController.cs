using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 定义当前类型。
/// </summary>
[ApiController]
[Route("api/v1/exports")]
public sealed class ExportsController(IExportCatalogQueryService exportCatalogQueryService) : QueryControllerBase
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [HttpPost("catalog")]
    [ProducesResponseType(typeof(ApiResponse<ExportCatalogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ExportCatalogResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ExportCatalogResponse>>> QueryCatalogAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ExportCatalogQueryRequest? request,
        [FromQuery] ExportCatalogQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        var resolvedRequest = ResolveRequest(request, queryRequest);
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(
                resolvedRequest.StartTimeLocal,
                resolvedRequest.EndTimeLocal,
                out var normalizedStart,
                out var normalizedEnd,
                out var validationMessage))
        {
            return BadRequest(ApiResponse<ExportCatalogResponse>.Fail(validationMessage));
        }

        var result = await exportCatalogQueryService.QueryAsync(new EverydayChain.Hub.Application.Models.ExportCatalogQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd
        }, cancellationToken);
        var response = new ExportCatalogResponse
        {
            StartTimeLocal = result.StartTimeLocal,
            EndTimeLocal = result.EndTimeLocal,
            GeneratedTimeLocal = result.GeneratedTimeLocal,
            Items = result.Items
                .Select(item => new ExportCatalogItemResponse
                {
                    Key = item.Key,
                    Scope = item.Scope,
                    Type = item.Type,
                    Content = item.Content,
                    Format = item.Format,
                    Endpoint = item.Endpoint,
                    UpdatedAt = item.UpdatedTimeLocal
                })
                .ToList()
        };
        return Ok(ApiResponse<ExportCatalogResponse>.Success(response, "Export catalog query succeeded."));
    }
}

