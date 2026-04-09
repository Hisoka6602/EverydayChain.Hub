using EverydayChain.Hub.Infrastructure.Services;

namespace EverydayChain.Hub.Tests.Repositories;

/// <summary>
/// 空实现分表预置器。
/// </summary>
public sealed class NoOpShardTableProvisioner : IShardTableProvisioner
{
    /// <inheritdoc/>
    public Task EnsureShardTableAsync(string suffix, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task EnsureShardTablesAsync(IEnumerable<string> suffixes, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
