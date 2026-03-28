namespace EverydayChain.Hub.Infrastructure.Persistence.Sharding;

/// <summary>
/// 按月份生成分表后缀的实现，后缀格式为 <c>_yyyyMM</c>，例如 <c>_202603</c>。
/// </summary>
public class MonthShardSuffixResolver : IShardSuffixResolver
{
    /// <inheritdoc/>
    public string Resolve(DateTimeOffset timestamp) => $"_{timestamp:yyyyMM}";

    /// <inheritdoc/>
    public IReadOnlyList<string> ResolveBootstrapSuffixes(DateTimeOffset localNow, int monthsAhead)
    {
        var result = new List<string> { Resolve(localNow) };
        for (var i = 1; i <= monthsAhead; i++)
        {
            result.Add(Resolve(localNow.AddMonths(i)));
        }

        return result;
    }
}
