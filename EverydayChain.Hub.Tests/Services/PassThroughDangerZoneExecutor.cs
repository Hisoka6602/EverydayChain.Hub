using EverydayChain.Hub.Infrastructure.Services;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 PassThroughDangerZoneExecutor 类型。
/// </summary>
internal sealed class PassThroughDangerZoneExecutor : IDangerZoneExecutor
{
    /// <summary>
    /// 执行 ExecuteAsync 方法。
    /// </summary>
    public Task ExecuteAsync(
        string operationName,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default,
        int? timeoutSecondsOverride = null)
    {
        // 步骤：执行 ExecuteAsync 方法的核心处理流程。
        return action(cancellationToken);
    }

    /// <summary>
    /// 执行当前业务方法。
    /// </summary>
    public Task<T> ExecuteAsync<T>(
        string operationName,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default,
        int? timeoutSecondsOverride = null)
    {
        // 步骤：执行当前业务方法的核心处理流程。
        return action(cancellationToken);
    }
}

