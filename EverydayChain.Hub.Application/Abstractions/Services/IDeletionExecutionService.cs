using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 删除执行服务接口。
/// </summary>
public interface IDeletionExecutionService
{
    /// <summary>
    /// 执行删除同步。
    /// </summary>
    /// <param name="context">同步执行上下文。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>删除执行结果。</returns>
    Task<SyncDeletionExecutionResult> ExecuteDeletionAsync(SyncExecutionContext context, CancellationToken ct);
}
