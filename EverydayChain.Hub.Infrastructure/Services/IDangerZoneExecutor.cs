namespace EverydayChain.Hub.Infrastructure.Services;

public interface IDangerZoneExecutor {
    Task ExecuteAsync(string operationName, Func<CancellationToken, Task> action, CancellationToken cancellationToken);
    Task<T> ExecuteAsync<T>(string operationName, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken);
}
