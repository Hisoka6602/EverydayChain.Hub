using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Aggregates.AuditLogs;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 提供保留期清理审计仓储的内存测试桩。
/// </summary>
internal sealed class InMemoryRetentionCleanupAuditLogRepository : IRetentionCleanupAuditLogRepository
{
    /// <summary>
    /// 获取已保存的审计记录集合。
    /// </summary>
    public List<RetentionCleanupAuditLogEntity> Items { get; } = [];

    /// <summary>
    /// 保存一条新的保留期清理审计记录。
    /// </summary>
    /// <param name="entity">待保存的审计实体。</param>
    /// <param name="ct">取消令牌。</param>
    public Task SaveAsync(RetentionCleanupAuditLogEntity entity, CancellationToken ct)
    {
        // 步骤：直接将实体追加到内存列表，便于测试断言后台清理是否完成落库。
        Items.Add(entity);
        return Task.CompletedTask;
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
    public Task<(int TotalCount, IReadOnlyList<RetentionCleanupAuditLogEntity> Items)> QueryAsync(
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
        // 步骤：按与正式仓储相同的过滤逻辑裁剪内存数据，确保查询服务测试行为一致。
        IEnumerable<RetentionCleanupAuditLogEntity> query = Items
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

        var orderedItems = query
            .OrderByDescending(item => item.StartedTimeLocal)
            .ThenByDescending(item => item.Id)
            .ToList();
        var pagedItems = orderedItems
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return Task.FromResult(((int)orderedItems.Count, (IReadOnlyList<RetentionCleanupAuditLogEntity>)pagedItems));
    }

    /// <summary>
    /// 更新一条已存在保留期清理审计记录的执行结果。
    /// </summary>
    /// <param name="auditLogId">审计记录标识。</param>
    /// <param name="executionStage">执行阶段。</param>
    /// <param name="scannedCount">扫描数量。</param>
    /// <param name="candidateCount">候选数量。</param>
    /// <param name="deletedCount">删除数量。</param>
    /// <param name="message">结果说明。</param>
    /// <param name="completedTimeLocal">完成时间，使用本地时间。</param>
    /// <param name="ct">取消令牌。</param>
    public Task UpdateResultAsync(
        string auditLogId,
        string executionStage,
        int scannedCount,
        int candidateCount,
        int deletedCount,
        string message,
        DateTime completedTimeLocal,
        CancellationToken ct)
    {
        // 步骤：定位内存记录并回写结果字段，模拟真实仓储的更新行为。
        var target = Items.First(item => item.Id == auditLogId);
        target.ExecutionStage = executionStage;
        target.ScannedCount = scannedCount;
        target.CandidateCount = candidateCount;
        target.DeletedCount = deletedCount;
        target.Message = message;
        target.CompletedTimeLocal = completedTimeLocal;
        return Task.CompletedTask;
    }
}
