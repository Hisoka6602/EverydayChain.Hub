using EverydayChain.Hub.Infrastructure.Services;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 危险动作执行器测试桩，直接执行委托。
/// </summary>
internal sealed class PassThroughDangerZoneExecutor : IDangerZoneExecutor
{
    /// <inheritdoc />
    public Task ExecuteAsync(
        string operationName,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default,
        int? timeoutSecondsOverride = null)
    {
        return action(cancellationToken);
    }

    /// <inheritdoc />
    public Task<T> ExecuteAsync<T>(
        string operationName,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default,
        int? timeoutSecondsOverride = null)
    {
        return action(cancellationToken);
    }
}
