using EverydayChain.Hub.Infrastructure.Services;

namespace EverydayChain.Hub.Tests.Repositories;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class NoOpShardTableProvisioner : IShardTableProvisioner
{
    public Task EnsureShardTableAsync(string suffix, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task EnsureShardTableAsync(string logicalTable, string suffix, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task EnsureShardTablesAsync(IEnumerable<string> suffixes, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

