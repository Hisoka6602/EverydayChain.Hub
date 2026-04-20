namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 波次摘要响应。
/// </summary>
public sealed class WaveSummaryResponse
{
    /// <summary>
    /// 波次号。
    /// </summary>
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 波次备注。
    /// </summary>
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 总件数。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 未分拣件数。
    /// </summary>
    public int UnsortedCount { get; set; }

    /// <summary>
    /// 分拣进度百分比。
    /// 可填写范围：0~100。
    /// </summary>
    public decimal SortedProgressPercent { get; set; }

    /// <summary>
    /// 回流件数。
    /// </summary>
    public int RecirculatedCount { get; set; }

    /// <summary>
    /// 异常件数。
    /// </summary>
    public int ExceptionCount { get; set; }
}
