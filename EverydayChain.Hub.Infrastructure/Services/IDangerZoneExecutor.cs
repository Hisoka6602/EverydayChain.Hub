namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 定义 IDangerZoneExecutor 类型。
/// </summary>
public interface IDangerZoneExecutor
{
    Task ExecuteAsync(
        string operationName,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default,
        int? timeoutSecondsOverride = null);

    Task<T> ExecuteAsync<T>(
        string operationName,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default,
        int? timeoutSecondsOverride = null);
}

