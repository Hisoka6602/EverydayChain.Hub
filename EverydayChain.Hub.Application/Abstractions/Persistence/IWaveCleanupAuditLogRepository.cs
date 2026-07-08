using EverydayChain.Hub.Domain.Aggregates.AuditLogs;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义波次清理审计记录仓储契约。
/// 该仓储专门用于持久化波次清理这类敏感操作的开始、完成和失败状态。
/// </summary>
public interface IWaveCleanupAuditLogRepository
{
    /// <summary>
    /// 保存一条新的波次清理审计记录。
    /// </summary>
    /// <param name="entity">待保存的审计实体。</param>
    /// <param name="ct">取消令牌。</param>
    Task SaveAsync(WaveCleanupAuditLogEntity entity, CancellationToken ct);

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
    Task UpdateResultAsync(
        string auditLogId,
        string executionStage,
        int identifiedCount,
        int cleanedCount,
        string message,
        DateTime completedTimeLocal,
        CancellationToken ct);
}
