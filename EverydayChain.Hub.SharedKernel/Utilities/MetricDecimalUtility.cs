using System.Diagnostics;

namespace EverydayChain.Hub.SharedKernel.Utilities;

/// <summary>
/// 集中处理监控指标的小数规整和耗时换算。
/// </summary>
public static class MetricDecimalUtility
{
    /// <summary>
    /// 存储默认小数位数。
    /// </summary>
    private const int DefaultScale = 3;

    /// <summary>
    /// 将小数规整为统一精度。
    /// </summary>
    /// <param name="value">待规整的小数值。</param>
    /// <returns>三位小数结果。</returns>
    public static decimal Round(decimal value)
    {
        return Math.Round(value, DefaultScale, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// 将 TimeSpan 换算为三位小数毫秒值。
    /// </summary>
    /// <param name="elapsed">待换算的耗时。</param>
    /// <returns>三位小数毫秒值。</returns>
    public static decimal ToMilliseconds(TimeSpan elapsed)
    {
        return Round(elapsed.Ticks / (decimal)TimeSpan.TicksPerMillisecond);
    }

    /// <summary>
    /// 将 Stopwatch 时间戳差值换算为三位小数秒值。
    /// </summary>
    /// <param name="ticks">Stopwatch 时间戳差值。</param>
    /// <returns>三位小数秒值。</returns>
    public static decimal StopwatchTicksToSeconds(long ticks)
    {
        if (ticks <= 0)
        {
            return 0.000M;
        }

        return Round(ticks / (decimal)Stopwatch.Frequency);
    }
}
