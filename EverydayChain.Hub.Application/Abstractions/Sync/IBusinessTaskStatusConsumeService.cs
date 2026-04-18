using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Abstractions.Sync;

/// <summary>
/// 业务任务状态驱动消费服务抽象，负责将远端状态驱动数据直接投影到业务任务主表。
/// </summary>
public interface IBusinessTaskStatusConsumeService
{
    /// <summary>
    /// 执行一轮状态驱动消费。
    /// </summary>
    /// <param name="definition">同步定义。</param>
    /// <param name="batchId">批次号。</param>
    /// <param name="window">时间窗口。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>消费结果。</returns>
    Task<RemoteStatusConsumeResult> ConsumeAsync(SyncTableDefinition definition, string batchId, SyncWindow window, CancellationToken ct);
}
