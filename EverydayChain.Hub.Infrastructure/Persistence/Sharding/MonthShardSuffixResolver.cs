namespace EverydayChain.Hub.Infrastructure.Persistence.Sharding;

/// <summary>
/// 定义当前类型。
/// </summary>
public class MonthShardSuffixResolver : IShardSuffixResolver
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public string Resolve(DateTimeOffset timestamp) => $"_{timestamp:yyyyMM}";

    public string ResolveLocal(DateTime localTime)
    {
        var normalizedLocalTime = localTime == DateTime.MinValue ? DateTime.Now : localTime;
        return Resolve(new DateTimeOffset(normalizedLocalTime));
    }

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


