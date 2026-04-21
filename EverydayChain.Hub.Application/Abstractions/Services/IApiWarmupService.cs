namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// API 启动预热服务抽象。
/// </summary>
public interface IApiWarmupService
{
    /// <summary>
    /// 执行 API 读路径预热。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    Task WarmupAsync(CancellationToken cancellationToken);
}
