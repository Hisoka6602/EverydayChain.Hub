namespace EverydayChain.Hub.Application.Utilities;

/// <summary>
/// 提供查询缓存时间桶归一化工具，降低滚动时间窗口查询的缓存碎片。
/// </summary>
public static class QueryCacheTimeBucket
{
    /// <summary>
    /// 将时间按指定秒级桶宽向下归一化；当桶宽小于等于 1 时返回原值。
    /// </summary>
    /// <param name="value">待归一化时间。</param>
    /// <param name="bucketSeconds">秒级桶宽。</param>
    /// <returns>归一化后的时间。</returns>
    public static DateTime Normalize(DateTime value, int bucketSeconds)
    {
        if (bucketSeconds <= 1)
        {
            return value;
        }

        var ticksFromDayStart = value.Ticks - value.Date.Ticks;
        var secondsFromDayStart = (int)(ticksFromDayStart / TimeSpan.TicksPerSecond);
        var normalizedSeconds = secondsFromDayStart - (secondsFromDayStart % bucketSeconds);
        return value.Date.AddSeconds(normalizedSeconds);
    }
}
