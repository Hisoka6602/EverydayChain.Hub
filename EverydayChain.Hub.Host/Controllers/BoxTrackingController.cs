using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// Exposes box-tracking queries backed by scan logs and local business tasks.
/// </summary>
[ApiController]
[Route("api/v1/box-tracking")]
public sealed class BoxTrackingController(IBoxTrackingQueryService boxTrackingQueryService) : QueryControllerBase
{
    /// <summary>
    /// Returns box-tracking rows within the requested local time range.
    /// </summary>
    [HttpPost("query")]
    [ProducesResponseType(typeof(ApiResponse<BoxTrackingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BoxTrackingResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<BoxTrackingResponse>>> QueryAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] BoxTrackingQueryRequest? request,
        [FromQuery] BoxTrackingQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        var resolvedRequest = ResolveRequest(request, queryRequest);
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(
                resolvedRequest.StartTimeLocal,
                resolvedRequest.EndTimeLocal,
                out var normalizedStart,
                out var normalizedEnd,
                out var validationMessage))
        {
            return BadRequest(ApiResponse<BoxTrackingResponse>.Fail(validationMessage));
        }

        if (resolvedRequest.PageNumber < 1 || resolvedRequest.PageNumber > 100000)
        {
            return BadRequest(ApiResponse<BoxTrackingResponse>.Fail("PageNumber must be between 1 and 100000."));
        }

        if (resolvedRequest.PageSize < 1 || resolvedRequest.PageSize > 1000)
        {
            return BadRequest(ApiResponse<BoxTrackingResponse>.Fail("PageSize must be between 1 and 1000."));
        }

        var result = await boxTrackingQueryService.QueryAsync(new EverydayChain.Hub.Application.Models.BoxTrackingQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd,
            BoxId = resolvedRequest.BoxId,
            Scanner = resolvedRequest.Scanner,
            ChuteCode = resolvedRequest.ChuteCode,
            PageNumber = resolvedRequest.PageNumber,
            PageSize = resolvedRequest.PageSize
        }, cancellationToken);

        var response = new BoxTrackingResponse
        {
            StartTimeLocal = result.StartTimeLocal,
            EndTimeLocal = result.EndTimeLocal,
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            Items = result.Items
                .Select(item => new BoxTrackingItemResponse
                {
                    BoxId = item.BoxId,
                    TaskCode = item.TaskCode,
                    WaveCode = item.WaveCode,
                    OrderId = item.OrderId,
                    StoreId = item.StoreId,
                    StoreName = item.StoreName,
                    ProductCode = item.ProductCode,
                    PickLocation = item.PickLocation,
                    Scanner = item.Scanner,
                    ScannedAt = item.ScannedAtLocal,
                    Chute = item.Chute,
                    Status = item.Status,
                    IsMatched = item.IsMatched,
                    FailureReason = item.FailureReason
                })
                .ToList()
        };

        return Ok(ApiResponse<BoxTrackingResponse>.Success(response, "Box tracking query succeeded."));
    }
}
