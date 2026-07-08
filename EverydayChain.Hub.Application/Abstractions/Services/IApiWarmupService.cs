namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义 IApiWarmupService 类型。
/// </summary>
public interface IApiWarmupService
{
    /// <summary>
    /// 执行 WarmupAsync 方法。
    /// </summary>
    Task WarmupAsync(CancellationToken cancellationToken);
}

