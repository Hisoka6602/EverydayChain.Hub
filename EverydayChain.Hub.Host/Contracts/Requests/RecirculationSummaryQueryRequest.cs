namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 表示回流汇总查询条件。
/// </summary>
public sealed class RecirculationSummaryQueryRequest
{
    /// <summary>
    /// 表示查询或统计开始时间（本地时间）。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 表示查询或统计结束时间（本地时间）。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }

    /// <summary>
    /// 表示格口编码。
    /// </summary>
    public string? ChuteCode { get; set; }

    /// <summary>
    /// 表示统计结果的排序方式。
    /// </summary>
    public string SortOrder { get; set; } = "Most";
}

