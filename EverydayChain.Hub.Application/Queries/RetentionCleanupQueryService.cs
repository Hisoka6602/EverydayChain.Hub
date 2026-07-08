using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Queries;

/// <summary>
/// 提供保留期清理审计查询服务。
/// </summary>
public sealed class RetentionCleanupQueryService(IRetentionCleanupAuditLogRepository retentionCleanupAuditLogRepository) : IRetentionCleanupQueryService
{
    /// <summary>
    /// 查询保留期清理审计记录。
    /// </summary>
    /// <param name="request">查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>分页查询结果。</returns>
    public async Task<RetentionCleanupAuditQueryResult> QueryAsync(RetentionCleanupAuditQueryRequest request, CancellationToken cancellationToken)
    {
        // 步骤：规范化分页与筛选参数，再委托仓储执行数据库查询，最后映射为应用层返回模型。
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 50 : request.PageSize;
        var queryResult = await retentionCleanupAuditLogRepository.QueryAsync(
            request.StartTimeLocal,
            request.EndTimeLocal,
            NormalizeOptionalValue(request.LogicalTableName),
            NormalizeOptionalValue(request.TargetCode),
            NormalizeOptionalValue(request.ExecutionStage),
            NormalizeOptionalValue(request.BatchId),
            pageNumber,
            pageSize,
            cancellationToken);

        return new RetentionCleanupAuditQueryResult
        {
            TotalCount = queryResult.TotalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = queryResult.Items
                .Select(item => new RetentionCleanupAuditItem
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
                    ExecutionStage = item.ExecutionStage,
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

    /// <summary>
    /// 规范化可选字符串筛选值。
    /// </summary>
    /// <param name="value">原始输入值。</param>
    /// <returns>去掉首尾空白后的值；若为空白则返回空引用。</returns>
    private static string? NormalizeOptionalValue(string? value)
    {
        // 步骤：统一将空白字符串收敛为空引用，避免仓储层重复处理无效筛选条件。
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
