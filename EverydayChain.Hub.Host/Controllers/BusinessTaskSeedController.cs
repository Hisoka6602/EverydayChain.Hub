using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 业务任务模拟补数控制器。
/// </summary>
[ApiController]
[Route("api/v1/business-task-seed")]
public sealed class BusinessTaskSeedController : ControllerBase
{
    /// <summary>
    /// 空请求体错误消息。
    /// </summary>
    private const string EmptyRequestBodyMessage = "模拟补数请求体不能为空。";

    /// <summary>
    /// 业务任务模拟补数应用服务。
    /// </summary>
    private readonly IBusinessTaskSeedService _businessTaskSeedService;

    /// <summary>
    /// 日志记录器。
    /// </summary>
    private readonly ILogger<BusinessTaskSeedController> _logger;

    /// <summary>
    /// 初始化业务任务模拟补数控制器。
    /// </summary>
    /// <param name="businessTaskSeedService">业务任务模拟补数应用服务。</param>
    /// <param name="logger">日志记录器。</param>
    public BusinessTaskSeedController(
        IBusinessTaskSeedService businessTaskSeedService,
        ILogger<BusinessTaskSeedController> logger)
    {
        _businessTaskSeedService = businessTaskSeedService;
        _logger = logger;
    }

    /// <summary>
    /// 手工提交条码并写入指定业务任务分表。
    /// </summary>
    /// <param name="request">模拟补数请求体。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>模拟补数执行结果。</returns>
    [HttpPost("manual")]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskSeedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskSeedResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskSeedResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BusinessTaskSeedResponse>>> ManualSeedAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] BusinessTaskSeedRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(ApiResponse<BusinessTaskSeedResponse>.Fail(EmptyRequestBodyMessage));
        }

        try
        {
            var result = await _businessTaskSeedService.ExecuteAsync(
                new BusinessTaskSeedCommand
                {
                    TargetTableName = request.TargetTableName,
                    Barcodes = request.Barcodes ?? []
                },
                cancellationToken);
            var response = BuildResponse(result);
            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse<BusinessTaskSeedResponse>.Fail(result.Message, response));
            }

            return Ok(ApiResponse<BusinessTaskSeedResponse>.Success(response, result.Message));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "业务任务模拟补数接口执行失败。");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<BusinessTaskSeedResponse>.Fail("模拟补数执行失败，请稍后重试。"));
        }
    }

    /// <summary>
    /// 构建响应对象。
    /// </summary>
    /// <param name="result">补数结果。</param>
    /// <returns>响应对象。</returns>
    private static BusinessTaskSeedResponse BuildResponse(BusinessTaskSeedResult result)
    {
        return new BusinessTaskSeedResponse
        {
            TargetTableName = result.TargetTableName,
            RequestedCount = result.RequestedCount,
            FilteredEmptyCount = result.FilteredEmptyCount,
            DeduplicatedCount = result.DeduplicatedCount,
            CandidateCount = result.CandidateCount,
            InsertedCount = result.InsertedCount,
            SkippedExistingCount = result.SkippedExistingCount
        };
    }
}
