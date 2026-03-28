namespace EverydayChain.Hub.Infrastructure.Persistence.Sharding;

public class MonthShardSuffixResolver : IShardSuffixResolver {
    public string Resolve(DateTimeOffset timestamp) => $"_{timestamp:yyyyMM}";

    public IReadOnlyList<string> ResolveBootstrapSuffixes(DateTimeOffset utcNow, int monthsAhead) {
        var result = new List<string> { Resolve(utcNow) };
        for (var i = 1; i <= monthsAhead; i++) {
            result.Add(Resolve(utcNow.AddMonths(i)));
        }

        return result;
    }
}
