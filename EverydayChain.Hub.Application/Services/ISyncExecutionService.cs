using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.SharedKernel.Utilities;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 同步执行服务接口。
/// </summary>
public interface ISyncExecutionService
{
    /// <summary>
    /// 执行单批次同步。
    /// </summary>
    /// <param name="context">执行上下文。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>同步批次结果。</returns>
    Task<SyncBatchResult> ExecuteBatchAsync(SyncExecutionContext context, CancellationToken ct);
}
