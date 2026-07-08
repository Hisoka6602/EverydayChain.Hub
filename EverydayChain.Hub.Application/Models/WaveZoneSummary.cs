namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 WaveZoneSummary 类型。
/// </summary>
public sealed class WaveZoneSummary
{
    /// <summary>
    /// 获取或设置 ZoneCode。
    /// </summary>
    public string ZoneCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 ZoneName。
    /// </summary>
    public string ZoneName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 TotalCount。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 获取或设置 UnsortedCount。
    /// </summary>
    public int UnsortedCount { get; set; }

    /// <summary>
    /// 获取或设置 SortedProgressPercent。
    /// </summary>
    public decimal SortedProgressPercent { get; set; }

    /// <summary>
    /// 获取或设置 RecirculatedCount。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 获取或设置 ExceptionCount。
    /// </summary>
    public int ExceptionCount { get; set; }
}

