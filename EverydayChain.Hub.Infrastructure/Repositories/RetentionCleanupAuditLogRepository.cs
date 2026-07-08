using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Aggregates.AuditLogs;
using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 提供保留期清理审计记录的持久化能力。
/// </summary>
public sealed class RetentionCleanupAuditLogRepository(IDbContextFactory<HubDbContext> contextFactory) : IRetentionCleanupAuditLogRepository
{
    /// <summary>
    /// 保存一条新的保留期清理审计记录。
    /// </summary>
    /// <param name="entity">待保存的审计实体。</param>
    /// <param name="ct">取消令牌。</param>
    public async Task SaveAsync(RetentionCleanupAuditLogEntity entity, CancellationToken ct)
    {
        // 步骤：将本轮自动清理目标的起始状态写入固定审计表，供后续结果回写与问题追踪使用。
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        db.Set<RetentionCleanupAuditLogEntity>().Add(entity);
        await db.SaveChangesAsync(ct);
    }

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
    /// <returns>返回总数与当前页记录集合。</returns>
    public async Task<(int TotalCount, IReadOnlyList<RetentionCleanupAuditLogEntity> Items)> QueryAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? logicalTableName,
        string? targetCode,
        string? executionStage,
        string? batchId,
        int pageNumber,
        int pageSize,
        CancellationToken ct)
    {
        // 步骤：先按时间窗口构建基础查询，再按可选条件缩小范围，最后按开始时间倒序分页返回最新记录。
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var query = db.Set<RetentionCleanupAuditLogEntity>()
            .AsNoTracking()
            .Where(item => item.StartedTimeLocal >= startTimeLocal && item.StartedTimeLocal <= endTimeLocal);

        if (!string.IsNullOrWhiteSpace(logicalTableName))
        {
            var normalizedLogicalTableName = logicalTableName.Trim();
            query = query.Where(item => item.LogicalTableName == normalizedLogicalTableName);
        }

        if (!string.IsNullOrWhiteSpace(targetCode))
        {
            var normalizedTargetCode = targetCode.Trim();
            query = query.Where(item => item.TargetCode == normalizedTargetCode);
        }

        if (!string.IsNullOrWhiteSpace(executionStage))
        {
            var normalizedExecutionStage = executionStage.Trim();
            query = query.Where(item => item.ExecutionStage == normalizedExecutionStage);
        }

        if (!string.IsNullOrWhiteSpace(batchId))
        {
            var normalizedBatchId = batchId.Trim();
            query = query.Where(item => item.BatchId == normalizedBatchId);
        }

        var totalCountLong = await query.LongCountAsync(ct);
        var items = await query
            .OrderByDescending(item => item.StartedTimeLocal)
            .ThenByDescending(item => item.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        var totalCount = totalCountLong > int.MaxValue ? int.MaxValue : Convert.ToInt32(totalCountLong);
        return (totalCount, items);
    }

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
    public async Task UpdateResultAsync(
        string auditLogId,
        string executionStage,
        int scannedCount,
        int candidateCount,
        int deletedCount,
        string message,
        DateTime completedTimeLocal,
        CancellationToken ct)
    {
        // 步骤：仅回写执行结果字段，避免覆盖任务开始时已经落下的目标配置与实例上下文。
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        await db.Set<RetentionCleanupAuditLogEntity>()
            .Where(item => item.Id == auditLogId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.ExecutionStage, executionStage)
                .SetProperty(item => item.ScannedCount, scannedCount)
                .SetProperty(item => item.CandidateCount, candidateCount)
                .SetProperty(item => item.DeletedCount, deletedCount)
                .SetProperty(item => item.Message, message)
                .SetProperty(item => item.CompletedTimeLocal, completedTimeLocal), ct);
    }
}
