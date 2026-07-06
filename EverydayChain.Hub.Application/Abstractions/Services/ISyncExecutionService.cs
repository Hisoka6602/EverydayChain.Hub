using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface ISyncExecutionService
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<SyncBatchResult> ExecuteBatchAsync(SyncExecutionContext context, CancellationToken ct);
}

