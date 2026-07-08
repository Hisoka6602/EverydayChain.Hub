using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Abstractions.Sync;

/// <summary>
/// 定义 IBusinessTaskStatusConsumeService 类型。
/// </summary>
public interface IBusinessTaskStatusConsumeService
{
    /// <summary>
    /// 执行 ConsumeAsync 方法。
    /// </summary>
    Task<RemoteStatusConsumeResult> ConsumeAsync(SyncTableDefinition definition, string batchId, SyncWindow window, CancellationToken ct);
}

