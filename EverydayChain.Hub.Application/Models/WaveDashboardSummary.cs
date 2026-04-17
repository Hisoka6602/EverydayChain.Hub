namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 波次维度统计摘要。
/// </summary>
public sealed class WaveDashboardSummary
{
    /// <summary>
    /// 波次编码。
    /// </summary>
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 波次总件数。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 波次未分拣数量。
    /// </summary>
    public int UnsortedCount { get; set; }

    /// <summary>
    /// 波次分拣进度（百分比）。
    /// </summary>
    public decimal SortedProgressPercent { get; set; }
}
