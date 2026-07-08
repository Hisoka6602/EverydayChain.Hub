using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 提供业务任务、异常件与回流件的统一明细查询接口，供前端列表页与诊断页复用。
/// </summary>
[ApiController]
[Route("api/v1/business-query")]
public sealed class BusinessTaskQueryController : QueryControllerBase
{
    /// <summary>
    /// 存储 _businessTaskReadService 字段。
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
    /// 查询业务任务明细，返回指定时间段内的任务状态、波次、条码、格口与扩展业务字段。
    /// </summary>
    /// <param name="request">请求体查询条件，支持按时间范围、波次、条码、码头与格口筛选。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>业务任务分页结果。</returns>
    [HttpPost("tasks")]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskQueryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskQueryResponse>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<BusinessTaskQueryResponse>>> QueryTasksAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] BusinessTaskQueryRequest? request,
        [FromQuery] BusinessTaskQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 QueryTasksAsync 方法的核心处理流程。
        return QueryCoreAsync(
            ResolveRequest(request, queryRequest),
            (payload, ct) => _businessTaskReadService.QueryTasksAsync(payload, ct),
            "业务任务查询成功。",
            cancellationToken);
    }

    /// <summary>
    /// 查询异常件明细，返回被识别为异常状态的业务任务列表，便于异常排查与人工处理。
    /// </summary>
    /// <param name="request">请求体查询条件。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异常件分页结果。</returns>
    [HttpPost("exceptions")]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskQueryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskQueryResponse>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<BusinessTaskQueryResponse>>> QueryExceptionsAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] BusinessTaskQueryRequest? request,
        [FromQuery] BusinessTaskQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 QueryExceptionsAsync 方法的核心处理流程。
        return QueryCoreAsync(
            ResolveRequest(request, queryRequest),
            (payload, ct) => _businessTaskReadService.QueryExceptionsAsync(payload, ct),
            "异常件查询成功。",
            cancellationToken);
    }

    /// <summary>
    /// 查询回流件明细，返回发生回流的业务任务列表，便于回流链路追踪与责任定位。
    /// </summary>
    /// <param name="request">请求体查询条件。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>回流件分页结果。</returns>
    [HttpPost("recirculations")]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskQueryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskQueryResponse>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<BusinessTaskQueryResponse>>> QueryRecirculationsAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] BusinessTaskQueryRequest? request,
        [FromQuery] BusinessTaskQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 QueryRecirculationsAsync 方法的核心处理流程。
        return QueryCoreAsync(
            ResolveRequest(request, queryRequest),
            (payload, ct) => _businessTaskReadService.QueryRecirculationsAsync(payload, ct),
            "回流记录查询成功。",
            cancellationToken);
    }

    /// <summary>
    /// 执行 QueryCoreAsync 方法。
    /// </summary>
    private async Task<ActionResult<ApiResponse<BusinessTaskQueryResponse>>> QueryCoreAsync(
        BusinessTaskQueryRequest request,
        Func<EverydayChain.Hub.Application.Models.BusinessTaskQueryRequest, CancellationToken, Task<EverydayChain.Hub.Application.Models.BusinessTaskQueryResult>> executor,
        string successMessage,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 QueryCoreAsync 方法的核心处理流程。
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
    /// 执行 TryValidateRequest 方法。
    /// </summary>
    private bool TryValidateRequest(
        BusinessTaskQueryRequest request,
        out DateTime normalizedStart,
        out DateTime normalizedEnd,
        out ActionResult<ApiResponse<BusinessTaskQueryResponse>>? validationResult)
    {
        // 步骤：执行 TryValidateRequest 方法的核心处理流程。
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

