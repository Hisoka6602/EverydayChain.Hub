using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 提供导出中心目录接口，用于返回前端可用的导出项清单、格式与对应端点信息。
/// </summary>
[ApiController]
[Route("api/v1/exports")]
public sealed class ExportsController(IExportCatalogQueryService exportCatalogQueryService) : QueryControllerBase
{
    /// <summary>
    /// 查询导出中心目录，返回指定时间段内系统支持的导出项、下载格式与接口地址。
    /// </summary>
    /// <param name="request">请求体查询条件，指定目录生成使用的时间范围。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>导出中心目录结果。</returns>
    [HttpPost("catalog")]
    [ProducesResponseType(typeof(ApiResponse<ExportCatalogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ExportCatalogResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ExportCatalogResponse>>> QueryCatalogAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ExportCatalogQueryRequest? request,
        [FromQuery] ExportCatalogQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 QueryCatalogAsync 方法的核心处理流程。
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

