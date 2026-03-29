namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 分表保留期执行服务接口。
/// </summary>
public interface IRetentionExecutionService
{
    /// <summary>
    /// 执行一次保留期清理任务。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>清理执行摘要文本。</returns>
    Task<string> ExecuteRetentionCleanupAsync(CancellationToken ct);
}
