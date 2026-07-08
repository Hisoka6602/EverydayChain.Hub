using EverydayChain.Hub.Domain.Aggregates.AuditLogs;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义保留期清理审计仓储契约。
/// 该仓储用于持久化自动保留期清理任务的开始、完成与失败记录，便于无人值守场景下追踪清理过程。
/// </summary>
public interface IRetentionCleanupAuditLogRepository
{
    /// <summary>
    /// 保存一条新的保留期清理审计记录。
    /// </summary>
    /// <param name="entity">待保存的审计实体。</param>
    /// <param name="ct">取消令牌。</param>
    Task SaveAsync(RetentionCleanupAuditLogEntity entity, CancellationToken ct);

    /// <summary>
    /// 按条件分页查询保留期清理审计记录。
    /// </summary>
    /// <param name="startTimeLocal">查询开始时间，使用本地时间。</param>
    /// <param name="endTimeLocal">查询结束时间，使用本地时间。</param>
    /// <param name="logicalTableName">逻辑表名筛选条件。</param>
    /// <param name="targetCode">目标编码筛选条件。</param>
    /// <param name="executionStage">执行阶段筛选条件。</param>
    /// <param name="batchId">批次号筛选条件。</param>
    /// <param name="pageNumber">页码，从 1 开始。</param>
    /// <param name="pageSize">每页条数。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>返回总数与当前页审计记录集合。</returns>
    Task<(int TotalCount, IReadOnlyList<RetentionCleanupAuditLogEntity> Items)> QueryAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? logicalTableName,
        string? targetCode,
        string? executionStage,
        string? batchId,
        int pageNumber,
        int pageSize,
        CancellationToken ct);

    /// <summary>
    /// 回写一条已存在保留期清理审计记录的执行结果。
    /// </summary>
    /// <param name="auditLogId">审计记录标识。</param>
    /// <param name="executionStage">执行阶段。</param>
    /// <param name="scannedCount">扫描数量。</param>
    /// <param name="candidateCount">候选数量。</param>
    /// <param name="deletedCount">删除数量。</param>
    /// <param name="message">执行结果说明。</param>
    /// <param name="completedTimeLocal">完成时间，使用本地时间。</param>
    /// <param name="ct">取消令牌。</param>
    Task UpdateResultAsync(
        string auditLogId,
        string executionStage,
        int scannedCount,
        int candidateCount,
        int deletedCount,
        string message,
        DateTime completedTimeLocal,
        CancellationToken ct);
}
