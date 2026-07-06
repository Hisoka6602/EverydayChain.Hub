using EverydayChain.Hub.Infrastructure.Services;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
internal sealed class PassThroughDangerZoneExecutor : IDangerZoneExecutor
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public Task ExecuteAsync(
        string operationName,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default,
        int? timeoutSecondsOverride = null)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        return action(cancellationToken);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public Task<T> ExecuteAsync<T>(
        string operationName,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default,
        int? timeoutSecondsOverride = null)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        return action(cancellationToken);
    }
}

