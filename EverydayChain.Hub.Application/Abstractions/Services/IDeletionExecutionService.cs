using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IDeletionExecutionService
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<SyncDeletionExecutionResult> ExecuteDeletionAsync(SyncExecutionContext context, CancellationToken ct);
}

