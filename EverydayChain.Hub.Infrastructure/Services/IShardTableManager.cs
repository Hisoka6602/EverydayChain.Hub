namespace EverydayChain.Hub.Infrastructure.Services;

public interface IShardTableManager {
    Task EnsureShardTableAsync(string suffix, CancellationToken cancellationToken);
    Task EnsureShardTablesAsync(IEnumerable<string> suffixes, CancellationToken cancellationToken);
}
