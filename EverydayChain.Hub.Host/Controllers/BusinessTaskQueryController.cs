using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 业务任务查询控制器，提供业务任务、异常件与回流记录分页查询能力。
/// </summary>
[ApiController]
[Route("api/v1/business-query")]
public sealed class BusinessTaskQueryController : ControllerBase
{
    /// <summary>
    /// 业务任务查询服务。
    /// </summary>
    private readonly IBusinessTaskReadService _businessTaskReadService;

    /// <summary>
    /// 初始化业务任务查询控制器。
    /// </summary>
    /// <param name="businessTaskReadService">业务任务查询服务。</param>
    public BusinessTaskQueryController(IBusinessTaskReadService businessTaskReadService)
    {
        _businessTaskReadService = businessTaskReadService;
    }

    /// <summary>
    /// 查询业务任务。
    /// 请求条件：时间范围必填且合法，分页参数需在允许范围内。
    /// 返回语义：返回业务任务口径分页结果；参数非法返回 400。
    /// </summary>
    /// <param name="request">查询请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>业务任务分页结果，包含总数、分页信息与任务明细列表。</returns>
    [HttpPost("tasks")]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskQueryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskQueryResponse>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<BusinessTaskQueryResponse>>> QueryTasksAsync([FromBody] BusinessTaskQueryRequest request, CancellationToken cancellationToken)
    {
        return QueryCoreAsync(request, (payload, ct) => _businessTaskReadService.QueryTasksAsync(payload, ct), "业务任务查询成功。", cancellationToken);
    }

    /// <summary>
    /// 查询异常件。
    /// 请求条件：与业务任务查询一致。
    /// 返回语义：仅返回异常件口径分页结果；参数非法返回 400。
    /// </summary>
    /// <param name="request">查询请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异常件分页结果，包含总数、分页信息与任务明细列表。</returns>
    [HttpPost("exceptions")]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskQueryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskQueryResponse>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<BusinessTaskQueryResponse>>> QueryExceptionsAsync([FromBody] BusinessTaskQueryRequest request, CancellationToken cancellationToken)
    {
        return QueryCoreAsync(request, (payload, ct) => _businessTaskReadService.QueryExceptionsAsync(payload, ct), "异常件查询成功。", cancellationToken);
    }

    /// <summary>
    /// 查询回流记录。
    /// 请求条件：与业务任务查询一致。
    /// 返回语义：仅返回回流口径分页结果；参数非法返回 400。
    /// </summary>
    /// <param name="request">查询请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>回流记录分页结果，包含总数、分页信息与任务明细列表。</returns>
    [HttpPost("recirculations")]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskQueryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskQueryResponse>), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ApiResponse<BusinessTaskQueryResponse>>> QueryRecirculationsAsync([FromBody] BusinessTaskQueryRequest request, CancellationToken cancellationToken)
    {
        return QueryCoreAsync(request, (payload, ct) => _businessTaskReadService.QueryRecirculationsAsync(payload, ct), "回流记录查询成功。", cancellationToken);
    }

    /// <summary>
    /// 查询主流程。
    /// </summary>
    /// <param name="request">查询请求。</param>
    /// <param name="executor">查询执行器。</param>
    /// <param name="successMessage">成功消息。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>查询结果。</returns>
    private async Task<ActionResult<ApiResponse<BusinessTaskQueryResponse>>> QueryCoreAsync(
        BusinessTaskQueryRequest request,
        Func<EverydayChain.Hub.Application.Models.BusinessTaskQueryRequest, CancellationToken, Task<EverydayChain.Hub.Application.Models.BusinessTaskQueryResult>> executor,
        string successMessage,
        CancellationToken cancellationToken)
    {
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
                    CreatedTimeLocal = item.CreatedTimeLocal
                })
                .ToList()
        };

        return Ok(ApiResponse<BusinessTaskQueryResponse>.Success(response, successMessage));
    }

    /// <summary>
    /// 校验查询请求。
    /// </summary>
    /// <param name="request">查询请求。</param>
    /// <param name="normalizedStart">规范化后的开始时间。</param>
    /// <param name="normalizedEnd">规范化后的结束时间。</param>
    /// <param name="validationResult">校验失败结果。</param>
    /// <returns>是否通过校验。</returns>
    private bool TryValidateRequest(
        BusinessTaskQueryRequest request,
        out DateTime normalizedStart,
        out DateTime normalizedEnd,
        out ActionResult<ApiResponse<BusinessTaskQueryResponse>>? validationResult)
    {
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
