namespace EverydayChain.Hub.Infrastructure.Persistence.Sharding;

/// <summary>
/// 分表后缀解析器接口，定义按时间戳生成分表后缀的契约。
/// </summary>
public interface IShardSuffixResolver
{
    /// <summary>
    /// 根据指定时间戳解析对应的分表后缀，例如 <c>_202603</c>。
    /// </summary>
    /// <param name="timestamp">目标时间戳（含本地偏移）。</param>
    /// <returns>分表后缀字符串。</returns>
    string Resolve(DateTimeOffset timestamp);

    /// <summary>
    /// 生成启动时需预创建的分表后缀列表，包含当前月及未来若干月。
    /// </summary>
    /// <param name="localNow">本地当前时间（含偏移）。</param>
    /// <param name="monthsAhead">需预创建的未来月份数。</param>
    /// <returns>分表后缀列表（只读）。</returns>
    IReadOnlyList<string> ResolveBootstrapSuffixes(DateTimeOffset localNow, int monthsAhead);
}
