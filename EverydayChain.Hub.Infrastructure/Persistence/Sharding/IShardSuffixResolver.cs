namespace EverydayChain.Hub.Infrastructure.Persistence.Sharding;

/// <summary>
/// 定义 IShardSuffixResolver 类型。
/// </summary>
public interface IShardSuffixResolver
{
    /// <summary>
    /// 执行 Resolve 方法。
    /// </summary>
    string Resolve(DateTimeOffset timestamp);

    /// <summary>
    /// 执行 ResolveLocal 方法。
    /// </summary>
    string ResolveLocal(DateTime localTime);

    /// <summary>
    /// 执行 ResolveBootstrapSuffixes 方法。
    /// </summary>
    IReadOnlyList<string> ResolveBootstrapSuffixes(DateTimeOffset localNow, int monthsAhead);
}

