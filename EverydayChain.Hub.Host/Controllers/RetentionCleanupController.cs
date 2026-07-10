using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 提供保留期清理审计查询接口，用于查看自动清理任务在无人值守期间的执行留痕。
/// </summary>
[ApiController]
[Route("api/v1/retention-cleanups")]
public sealed class RetentionCleanupController(IRetentionCleanupQueryService retentionCleanupQueryService) : QueryControllerBase
{
    /// <summary>
    /// 查询保留期清理审计记录，返回指定时间范围内每个清理目标的执行阶段、扫描数量、候选数量和删除数量。
    /// </summary>
    /// <param name="request">请求体查询条件。</param>
    /// <param name="queryRequest">查询字符串查询条件，字段语义与请求体一致，便于浏览器直接调试。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>保留期清理审计分页结果。</returns>
    [HttpPost("query")]
    [ProducesResponseType(typeof(ApiResponse<RetentionCleanupAuditQueryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RetentionCleanupAuditQueryResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<RetentionCleanupAuditQueryResponse>>> QueryAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RetentionCleanupAuditQueryRequest? request,
        [FromQuery] RetentionCleanupAuditQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：统一解析请求来源并校验时间范围、分页参数，避免自动清理审计查询进入无界扫描。
        var resolvedRequest = ResolveRequest(request, queryRequest);
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(
                resolvedRequest.StartTimeLocal,
                resolvedRequest.EndTimeLocal,
                out var normalizedStartTime,
                out var normalizedEndTime,
                out var validationMessage))
        {
            return BadRequest(ApiResponse<RetentionCleanupAuditQueryResponse>.Fail(validationMessage));
        }

        if (resolvedRequest.PageNumber < 1 || resolvedRequest.PageNumber > 100000)
        {
            return BadRequest(ApiResponse<RetentionCleanupAuditQueryResponse>.Fail("页码范围必须在 1 到 100000 之间。"));
        }

        if (resolvedRequest.PageSize < 1 || resolvedRequest.PageSize > 1000)
        {
            return BadRequest(ApiResponse<RetentionCleanupAuditQueryResponse>.Fail("页大小范围必须在 1 到 1000 之间。"));
        }

        var result = await retentionCleanupQueryService.QueryAsync(
            new EverydayChain.Hub.Application.Models.RetentionCleanupAuditQueryRequest
            {
                StartTimeLocal = normalizedStartTime,
                EndTimeLocal = normalizedEndTime,
                LogicalTableName = resolvedRequest.LogicalTableName,
                TargetCode = resolvedRequest.TargetCode,
                ExecutionStage = resolvedRequest.ExecutionStage,
                BatchId = resolvedRequest.BatchId,
                PageNumber = resolvedRequest.PageNumber,
                PageSize = resolvedRequest.PageSize
            },
            cancellationToken);

        return Ok(ApiResponse<RetentionCleanupAuditQueryResponse>.Success(BuildResponse(result), "保留期清理审计查询成功。"));
    }

    /// <summary>
    /// 构建保留期清理审计响应体。
    /// </summary>
    /// <param name="result">应用层查询结果。</param>
    /// <returns>对外响应体。</returns>
    private static RetentionCleanupAuditQueryResponse BuildResponse(EverydayChain.Hub.Application.Models.RetentionCleanupAuditQueryResult result)
    {
        return new RetentionCleanupAuditQueryResponse
        {
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            Items = result.Items
                .Select(item => new RetentionCleanupAuditItemResponse
                {
                    Id = item.Id,
                    BatchId = item.BatchId,
                    TargetCode = item.TargetCode,
                    LogicalTableName = item.LogicalTableName,
                    RetentionMode = item.RetentionMode,
                    TimeColumnName = item.TimeColumnName,
                    KeepMonths = item.KeepMonths,
                    IsDryRun = item.IsDryRun,
                    AllowDelete = item.AllowDelete,
                    ExecutionStage = LocalizeExecutionStage(item.ExecutionStage),
                    ScannedCount = item.ScannedCount,
                    CandidateCount = item.CandidateCount,
                    DeletedCount = item.DeletedCount,
                    Message = item.Message,
                    InstanceId = item.InstanceId,
                    ThresholdTimeLocal = item.ThresholdTimeLocal,
                    StartedTimeLocal = item.StartedTimeLocal,
                    CompletedTimeLocal = item.CompletedTimeLocal
                })
                .ToList()
        };
    }

    private static string LocalizeExecutionStage(string? executionStage)
    {
        return executionStage switch
        {
            "Started" => "已开始",
            "Completed" => "已完成",
            "Failed" => "失败",
            _ => executionStage ?? string.Empty
        };
    }
}
