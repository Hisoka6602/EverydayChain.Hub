namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 WaveSummaryQueryResult 类型。
/// </summary>
public sealed class WaveSummaryQueryResult
{
    /// <summary>
    /// 获取或设置 WaveCode。
    /// </summary>
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 WaveRemark。
    /// </summary>
    public string? WaveRemark { get; set; }

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

