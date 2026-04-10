using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Abstractions.Sync;

/// <summary>
/// 状态驱动消费服务接口，串联 Oracle 状态行读取、SQL Server 追加写入与可选远端状态回写。
/// 仅在 SyncMode = StatusDriven 时由 SyncExecutionService 调用。
/// </summary>
public interface IRemoteStatusConsumeService
{
    /// <summary>
    /// 执行一轮状态驱动消费（读取所有待处理页直到读空）。
    /// </summary>
    /// <param name="definition">同步表定义，包含源表信息与状态消费配置。</param>
    /// <param name="batchId">当前批次编号，用于日志关联。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>本轮消费结果统计。</returns>
    Task<RemoteStatusConsumeResult> ConsumeAsync(SyncTableDefinition definition, string batchId, CancellationToken ct);
}
