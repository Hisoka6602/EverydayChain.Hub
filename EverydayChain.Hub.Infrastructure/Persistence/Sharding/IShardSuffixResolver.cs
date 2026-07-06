namespace EverydayChain.Hub.Infrastructure.Persistence.Sharding;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IShardSuffixResolver
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    string Resolve(DateTimeOffset timestamp);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    string ResolveLocal(DateTime localTime);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    IReadOnlyList<string> ResolveBootstrapSuffixes(DateTimeOffset localNow, int monthsAhead);
}

