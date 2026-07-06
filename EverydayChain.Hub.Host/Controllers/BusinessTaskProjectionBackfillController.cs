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
[Route("api/v1/business-task-backfills")]
public sealed class BusinessTaskProjectionBackfillController(
    IBusinessTaskProjectionBackfillService businessTaskProjectionBackfillService,
    ILogger<BusinessTaskProjectionBackfillController> logger) : ControllerBase
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string EmptyRequestBodyMessage = "Request body cannot be null.";

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [HttpPost("projection/summary")]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskProjectionBackfillPreviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskProjectionBackfillPreviewResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskProjectionBackfillPreviewResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BusinessTaskProjectionBackfillPreviewResponse>>> PreviewAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] BusinessTaskProjectionBackfillPreviewRequest? request,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        if (request is null)
        {
            return BadRequest(ApiResponse<BusinessTaskProjectionBackfillPreviewResponse>.Fail(EmptyRequestBodyMessage));
        }

        try
        {
            var result = await businessTaskProjectionBackfillService.PreviewAsync(
                new BusinessTaskProjectionBackfillPreviewCommand
                {
                    StartTimeLocal = request.StartTimeLocal,
                    EndTimeLocal = request.EndTimeLocal,
                    TableCode = request.TableCode
                },
                cancellationToken);
            var response = BuildPreviewResponse(result);
            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse<BusinessTaskProjectionBackfillPreviewResponse>.Fail(result.Message, response));
            }

            return Ok(ApiResponse<BusinessTaskProjectionBackfillPreviewResponse>.Success(response, result.Message));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Business-task projection preview API failed.");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<BusinessTaskProjectionBackfillPreviewResponse>.Fail("Business-task projection preview failed."));
        }
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [HttpPost("projection")]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskProjectionBackfillResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskProjectionBackfillResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskProjectionBackfillResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BusinessTaskProjectionBackfillResponse>>> ExecuteAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] BusinessTaskProjectionBackfillRequest? request,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        if (request is null)
        {
            return BadRequest(ApiResponse<BusinessTaskProjectionBackfillResponse>.Fail(EmptyRequestBodyMessage));
        }

        try
        {
            var result = await businessTaskProjectionBackfillService.ExecuteAsync(
                new BusinessTaskProjectionBackfillCommand
                {
                    StartTimeLocal = request.StartTimeLocal,
                    EndTimeLocal = request.EndTimeLocal,
                    TableCode = request.TableCode,
                    MaxCount = request.MaxCount,
                    BatchSize = request.BatchSize
                },
                cancellationToken);
            var response = BuildResponse(result);
            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse<BusinessTaskProjectionBackfillResponse>.Fail(result.Message, response));
            }

            return Ok(ApiResponse<BusinessTaskProjectionBackfillResponse>.Success(response, result.Message));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Business-task projection backfill API failed.");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<BusinessTaskProjectionBackfillResponse>.Fail("Business-task projection backfill failed."));
        }
    }

    private static BusinessTaskProjectionBackfillResponse BuildResponse(BusinessTaskProjectionBackfillResult result)
    {
        return new BusinessTaskProjectionBackfillResponse
        {
            ProcessedTableCount = result.ProcessedTableCount,
            CandidateCount = result.CandidateCount,
            RemoteRowCount = result.RemoteRowCount,
            ProjectedCount = result.ProjectedCount,
            UpdatedCount = result.UpdatedCount,
            MissingRemoteCount = result.MissingRemoteCount,
            Tables = result.Tables
                .Select(item => new BusinessTaskProjectionBackfillTableResponse
                {
                    TableCode = item.TableCode,
                    CandidateCount = item.CandidateCount,
                    RemoteRowCount = item.RemoteRowCount,
                    ProjectedCount = item.ProjectedCount,
                    UpdatedCount = item.UpdatedCount,
                    MissingRemoteCount = item.MissingRemoteCount
                })
                .ToList()
        };
    }

    private static BusinessTaskProjectionBackfillPreviewResponse BuildPreviewResponse(BusinessTaskProjectionBackfillPreviewResult result)
    {
        return new BusinessTaskProjectionBackfillPreviewResponse
        {
            ProcessedTableCount = result.ProcessedTableCount,
            CandidateCount = result.CandidateCount,
            Tables = result.Tables
                .Select(item => new BusinessTaskProjectionBackfillPreviewTableResponse
                {
                    TableCode = item.TableCode,
                    CandidateCount = item.CandidateCount,
                    MissingOrderIdCount = item.MissingOrderIdCount,
                    MissingStoreIdCount = item.MissingStoreIdCount,
                    MissingStoreNameCount = item.MissingStoreNameCount,
                    MissingProductCodeCount = item.MissingProductCodeCount,
                    MissingPickLocationCount = item.MissingPickLocationCount
                })
                .ToList()
        };
    }
}

