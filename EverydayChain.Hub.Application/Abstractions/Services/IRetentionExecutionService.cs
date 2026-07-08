namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义 IRetentionExecutionService 类型。
/// </summary>
public interface IRetentionExecutionService
{
    /// <summary>
    /// 执行 ExecuteRetentionCleanupAsync 方法。
    /// </summary>
    Task<string> ExecuteRetentionCleanupAsync(CancellationToken ct);
}

