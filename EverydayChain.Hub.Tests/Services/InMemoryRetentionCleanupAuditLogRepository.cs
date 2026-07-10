using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Aggregates.AuditLogs;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 提供保留期清理审计仓储的内存测试替身。
/// </summary>
internal sealed class InMemoryRetentionCleanupAuditLogRepository : IRetentionCleanupAuditLogRepository
{
    /// <summary>
    /// 获取或设置 QueryCallCount。
    /// </summary>
    public int QueryCallCount { get; private set; }

    /// <summary>
    /// 获取已保存的审计记录集合。
    /// </summary>
    public List<RetentionCleanupAuditLogEntity> Items { get; } = [];

    /// <summary>
    /// 保存一条新的保留期清理审计记录。
    /// </summary>
    public Task SaveAsync(RetentionCleanupAuditLogEntity entity, CancellationToken ct)
    {
        Items.Add(entity);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 按条件分页查询保留期清理审计记录。
    /// </summary>
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
        // 步骤：按时间范围和可选筛选条件过滤内存数据，再返回稳定排序后的分页结果。
        QueryCallCount++;

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
