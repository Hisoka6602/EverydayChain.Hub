using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Aggregates.AuditLogs;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 提供波次清理审计仓储测试桩。
/// </summary>
internal sealed class InMemoryWaveCleanupAuditLogRepository : IWaveCleanupAuditLogRepository
{
    /// <summary>
    /// 获取已保存的审计记录集合。
    /// </summary>
    public List<WaveCleanupAuditLogEntity> Items { get; } = [];

    /// <summary>
    /// 保存一条新的波次清理审计记录。
    /// </summary>
    /// <param name="entity">待保存的实体。</param>
    /// <param name="ct">取消令牌。</param>
    public Task SaveAsync(WaveCleanupAuditLogEntity entity, CancellationToken ct)
    {
        // 步骤：直接将实体缓存到内存列表，供测试断言使用。
        Items.Add(entity);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 更新一条已有波次清理审计记录的执行结果。
    /// </summary>
    /// <param name="auditLogId">审计记录标识。</param>
    /// <param name="executionStage">执行阶段。</param>
    /// <param name="identifiedCount">识别数量。</param>
    /// <param name="cleanedCount">变更数量。</param>
    /// <param name="message">结果说明。</param>
    /// <param name="completedTimeLocal">完成时间。</param>
    /// <param name="ct">取消令牌。</param>
    public Task UpdateResultAsync(
        string auditLogId,
        string executionStage,
        int identifiedCount,
        int cleanedCount,
        string message,
        DateTime completedTimeLocal,
        CancellationToken ct)
    {
        // 步骤：定位内存记录并更新执行结果字段。
        var target = Items.First(item => item.Id == auditLogId);
        target.ExecutionStage = executionStage;
        target.IdentifiedCount = identifiedCount;
        target.CleanedCount = cleanedCount;
        target.Message = message;
        target.CompletedTimeLocal = completedTimeLocal;
        return Task.CompletedTask;
    }
}
