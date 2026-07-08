using EverydayChain.Hub.Infrastructure.Services;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 RecordingShardTableProvisioner 类型。
/// </summary>
public sealed class RecordingShardTableProvisioner : IShardTableProvisioner
{
    /// <summary>
    /// 获取或设置 EnsuredSuffixes。
    /// </summary>
    public List<string> EnsuredSuffixes { get; } = [];

    public Task EnsureShardTableAsync(string suffix, CancellationToken cancellationToken)
    {
        EnsuredSuffixes.Add(suffix);
        return Task.CompletedTask;
    }

    public Task EnsureShardTableAsync(string logicalTable, string suffix, CancellationToken cancellationToken)
    {
        EnsuredSuffixes.Add($"{logicalTable}:{suffix}");
        return Task.CompletedTask;
    }

    public Task EnsureShardTablesAsync(IEnumerable<string> suffixes, CancellationToken cancellationToken)
    {
        foreach (var suffix in suffixes)
        {
            EnsuredSuffixes.Add(suffix);
        }

        return Task.CompletedTask;
    }
}

