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
[Route("api/v1/business-query")]
public sealed class BusinessTaskQueryController : QueryControllerBase
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly IBusinessTaskReadService _businessTaskReadService;

    /// <summary>
    /// 初始化业务任务查询控制器。
    /// </summary>
    /// <param name="businessTaskReadService">业务任务读取服务。</param>
    public BusinessTaskQueryController(IBusinessTaskReadService businessTaskReadService)
    {
        _businessTaskReadService = businessTaskReadService;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [HttpPost("tasks")]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskQueryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskQueryResponse>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<BusinessTaskQueryResponse>>> QueryTasksAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] BusinessTaskQueryRequest? request,
        [FromQuery] BusinessTaskQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        return QueryCoreAsync(
            ResolveRequest(request, queryRequest),
            (payload, ct) => _businessTaskReadService.QueryTasksAsync(payload, ct),
            "业务任务查询成功。",
            cancellationToken);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [HttpPost("exceptions")]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskQueryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskQueryResponse>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<BusinessTaskQueryResponse>>> QueryExceptionsAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] BusinessTaskQueryRequest? request,
        [FromQuery] BusinessTaskQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        return QueryCoreAsync(
            ResolveRequest(request, queryRequest),
            (payload, ct) => _businessTaskReadService.QueryExceptionsAsync(payload, ct),
            "异常件查询成功。",
            cancellationToken);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [HttpPost("recirculations")]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskQueryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskQueryResponse>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<BusinessTaskQueryResponse>>> QueryRecirculationsAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] BusinessTaskQueryRequest? request,
        [FromQuery] BusinessTaskQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        return QueryCoreAsync(
            ResolveRequest(request, queryRequest),
            (payload, ct) => _businessTaskReadService.QueryRecirculationsAsync(payload, ct),
            "回流记录查询成功。",
            cancellationToken);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private async Task<ActionResult<ApiResponse<BusinessTaskQueryResponse>>> QueryCoreAsync(
        BusinessTaskQueryRequest request,
        Func<EverydayChain.Hub.Application.Models.BusinessTaskQueryRequest, CancellationToken, Task<EverydayChain.Hub.Application.Models.BusinessTaskQueryResult>> executor,
        string successMessage,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        if (!TryValidateRequest(request, out var normalizedStart, out var normalizedEnd, out var validationResult))
        {
            return validationResult!;
        }

        var appRequest = new EverydayChain.Hub.Application.Models.BusinessTaskQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd,
            WaveCode = request.WaveCode,
            Barcode = request.Barcode,
            DockCode = request.DockCode,
            ChuteCode = request.ChuteCode,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            LastCreatedTimeLocal = request.LastCreatedTimeLocal,
            LastId = request.LastId
        };

        var queryResult = await executor(appRequest, cancellationToken);
        var response = new BusinessTaskQueryResponse
        {
            TotalCount = queryResult.TotalCount,
            PageNumber = queryResult.PageNumber,
            PageSize = queryResult.PageSize,
            HasMore = queryResult.HasMore,
            NextLastCreatedTimeLocal = queryResult.NextLastCreatedTimeLocal,
            NextLastId = queryResult.NextLastId,
            PaginationMode = queryResult.PaginationMode,
            Items = queryResult.Items
                .Select(item => new BusinessTaskItemResponse
                {
                    TaskCode = item.TaskCode,
                    Barcode = item.Barcode,
                    WaveCode = item.WaveCode,
                    SourceType = (int)item.SourceType,
                    Status = (int)item.Status,
                    TargetChuteCode = item.TargetChuteCode,
                    ActualChuteCode = item.ActualChuteCode,
                    DockCode = item.DockCode,
                    IsRecirculated = item.IsRecirculated,
                    IsException = item.IsException,
                    CreatedTimeLocal = item.CreatedTimeLocal,
                    OrderId = item.OrderId,
                    StoreId = item.StoreId,
                    StoreName = item.StoreName,
                    ProductCode = item.ProductCode,
                    PickLocation = item.PickLocation
                })
                .ToList()
        };

        return Ok(ApiResponse<BusinessTaskQueryResponse>.Success(response, successMessage));
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private bool TryValidateRequest(
        BusinessTaskQueryRequest request,
        out DateTime normalizedStart,
        out DateTime normalizedEnd,
        out ActionResult<ApiResponse<BusinessTaskQueryResponse>>? validationResult)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        normalizedStart = default;
        normalizedEnd = default;
        validationResult = null;

        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(request.StartTimeLocal, request.EndTimeLocal, out normalizedStart, out normalizedEnd, out var validationMessage))
        {
            validationResult = BadRequest(ApiResponse<BusinessTaskQueryResponse>.Fail(validationMessage));
            return false;
        }

        if (request.PageNumber < 1 || request.PageNumber > 100000)
        {
            validationResult = BadRequest(ApiResponse<BusinessTaskQueryResponse>.Fail("页码范围必须在 1 到 100000 之间。"));
            return false;
        }

        if (request.PageSize < 1 || request.PageSize > 1000)
        {
            validationResult = BadRequest(ApiResponse<BusinessTaskQueryResponse>.Fail("页大小范围必须在 1 到 1000 之间。"));
            return false;
        }

        if (request.LastCreatedTimeLocal.HasValue != request.LastId.HasValue)
        {
            validationResult = BadRequest(ApiResponse<BusinessTaskQueryResponse>.Fail("游标分页参数 LastCreatedTimeLocal 与 LastId 必须同时传入或同时为空。"));
            return false;
        }

        if (request.LastId.HasValue && request.LastId.Value <= 0)
        {
            validationResult = BadRequest(ApiResponse<BusinessTaskQueryResponse>.Fail("游标分页参数 LastId 必须大于 0。"));
            return false;
        }

        return true;
    }
}

