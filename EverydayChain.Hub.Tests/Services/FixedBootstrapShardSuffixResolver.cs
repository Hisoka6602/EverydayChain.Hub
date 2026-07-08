using EverydayChain.Hub.Infrastructure.Persistence.Sharding;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 FixedBootstrapShardSuffixResolver 类型。
/// </summary>
public sealed class FixedBootstrapShardSuffixResolver(IReadOnlyList<string> bootstrapSuffixes) : IShardSuffixResolver
{
    public string Resolve(DateTimeOffset timestamp)
    {
        return "_202604";
    }

    public string ResolveLocal(DateTime localTime)
    {
        return "_202604";
    }

    public IReadOnlyList<string> ResolveBootstrapSuffixes(DateTimeOffset now, int monthsAhead)
    {
        return bootstrapSuffixes;
    }
}

