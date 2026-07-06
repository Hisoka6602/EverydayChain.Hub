namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IApiWarmupService
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task WarmupAsync(CancellationToken cancellationToken);
}

