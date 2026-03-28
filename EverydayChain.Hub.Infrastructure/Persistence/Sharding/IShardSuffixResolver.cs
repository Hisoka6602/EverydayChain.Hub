namespace EverydayChain.Hub.Infrastructure.Persistence.Sharding;

public interface IShardSuffixResolver {
    string Resolve(DateTimeOffset timestamp);
    IReadOnlyList<string> ResolveBootstrapSuffixes(DateTimeOffset localNow, int monthsAhead);
}
