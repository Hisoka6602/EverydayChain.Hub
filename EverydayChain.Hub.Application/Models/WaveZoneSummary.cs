namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class WaveZoneSummary
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string ZoneCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string ZoneName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int UnsortedCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public decimal SortedProgressPercent { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int ExceptionCount { get; set; }
}

