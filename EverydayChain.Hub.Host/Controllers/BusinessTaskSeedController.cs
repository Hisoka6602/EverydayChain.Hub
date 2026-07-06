using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 定义当前类型。
/// </summary>
[ApiController]
[Route("api/v1/business-task-seed")]
public sealed class BusinessTaskSeedController : ControllerBase
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string EmptyRequestBodyMessage = "模拟补数请求体不能为空。";

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly IBusinessTaskSeedService _businessTaskSeedService;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly ILogger<BusinessTaskSeedController> _logger;

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public BusinessTaskSeedController(
        IBusinessTaskSeedService businessTaskSeedService,
        ILogger<BusinessTaskSeedController> logger)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        _businessTaskSeedService = businessTaskSeedService;
        _logger = logger;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [HttpPost("manual")]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskSeedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskSeedResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskSeedResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BusinessTaskSeedResponse>>> ManualSeedAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] BusinessTaskSeedRequest? request,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
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
            SkippedExistingCount = result.SkippedExistingCount,
            InsertedBarcodes = result.InsertedBarcodes,
            SkippedBarcodes = result.SkippedBarcodes
        };
    }
}

