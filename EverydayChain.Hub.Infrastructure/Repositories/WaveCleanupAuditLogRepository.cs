using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Aggregates.AuditLogs;
using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 提供波次清理审计记录的持久化能力。
/// </summary>
public sealed class WaveCleanupAuditLogRepository(IDbContextFactory<HubDbContext> contextFactory) : IWaveCleanupAuditLogRepository
{
    /// <summary>
    /// 保存一条新的波次清理审计记录。
    /// </summary>
    /// <param name="entity">待保存的审计实体。</param>
    /// <param name="ct">取消令牌。</param>
    public async Task SaveAsync(WaveCleanupAuditLogEntity entity, CancellationToken ct)
    {
        // 步骤：使用非分表审计表持久化敏感操作开始记录。
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        db.Set<WaveCleanupAuditLogEntity>().Add(entity);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// 更新一条已有波次清理审计记录的执行结果。
    /// </summary>
    /// <param name="auditLogId">审计记录标识。</param>
    /// <param name="executionStage">执行阶段。</param>
    /// <param name="identifiedCount">识别出的任务数量。</param>
    /// <param name="cleanedCount">实际变更的任务数量。</param>
    /// <param name="message">执行结果说明。</param>
    /// <param name="completedTimeLocal">完成时间。</param>
    /// <param name="ct">取消令牌。</param>
    public async Task UpdateResultAsync(
        string auditLogId,
        string executionStage,
        int identifiedCount,
        int cleanedCount,
        string message,
        DateTime completedTimeLocal,
        CancellationToken ct)
    {
        // 步骤：只更新结果字段，避免覆盖开始时记录的请求来源信息。
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        await db.Set<WaveCleanupAuditLogEntity>()
            .Where(x => x.Id == auditLogId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.ExecutionStage, executionStage)
                .SetProperty(x => x.IdentifiedCount, identifiedCount)
                .SetProperty(x => x.CleanedCount, cleanedCount)
                .SetProperty(x => x.Message, message)
                .SetProperty(x => x.CompletedTimeLocal, completedTimeLocal), ct);
    }
}
