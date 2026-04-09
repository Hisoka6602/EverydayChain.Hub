using EverydayChain.Hub.Infrastructure.Services;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 记录建表调用的分表预建器桩实现。
/// </summary>
public sealed class RecordingShardTableProvisioner : IShardTableProvisioner
{
    /// <summary>已触发建表的后缀列表。</summary>
    public List<string> EnsuredSuffixes { get; } = [];

    /// <inheritdoc />
    public Task EnsureShardTableAsync(string suffix, CancellationToken cancellationToken)
    {
        EnsuredSuffixes.Add(suffix);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task EnsureShardTablesAsync(IEnumerable<string> suffixes, CancellationToken cancellationToken)
    {
        foreach (var suffix in suffixes)
        {
            EnsuredSuffixes.Add(suffix);
        }

        return Task.CompletedTask;
    }
}
