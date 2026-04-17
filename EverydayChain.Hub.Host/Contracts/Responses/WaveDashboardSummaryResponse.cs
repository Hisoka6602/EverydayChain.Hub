namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 波次维度统计摘要响应。
/// </summary>
public sealed class WaveDashboardSummaryResponse
{
    /// <summary>
    /// 波次编码。
    /// 空字符串表示数据源未返回有效波次号。
    /// </summary>
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 当前波次总件数。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 当前波次未分拣数量。
    /// </summary>
    public int UnsortedCount { get; set; }

    /// <summary>
    /// 当前波次分拣进度百分比。
    /// 可填写范围：0~100。
    /// </summary>
    public decimal SortedProgressPercent { get; set; }
}
