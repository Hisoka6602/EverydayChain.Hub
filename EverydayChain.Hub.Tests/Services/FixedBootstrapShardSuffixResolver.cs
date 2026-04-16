using EverydayChain.Hub.Infrastructure.Persistence.Sharding;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 固定返回启动后缀集合的分片后缀解析器测试替身。
/// </summary>
/// <param name="bootstrapSuffixes">启动后缀集合。</param>
public sealed class FixedBootstrapShardSuffixResolver(IReadOnlyList<string> bootstrapSuffixes) : IShardSuffixResolver
{
    /// <inheritdoc/>
    public string Resolve(DateTimeOffset timestamp)
    {
        return "_202604";
    }

    /// <inheritdoc/>
    public string ResolveLocal(DateTime localTime)
    {
        return "_202604";
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> ResolveBootstrapSuffixes(DateTimeOffset now, int monthsAhead)
    {
        return bootstrapSuffixes;
    }
}
