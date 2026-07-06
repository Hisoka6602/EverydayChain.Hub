using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Abstractions.Sync;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IBusinessTaskStatusConsumeService
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<RemoteStatusConsumeResult> ConsumeAsync(SyncTableDefinition definition, string batchId, SyncWindow window, CancellationToken ct);
}

