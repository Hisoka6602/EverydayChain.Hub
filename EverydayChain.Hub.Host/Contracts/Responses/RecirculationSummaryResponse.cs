namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示回流汇总查询结果。
/// </summary>
public sealed class RecirculationSummaryResponse
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
    /// 表示本次统计实际使用的格口编码。
    /// </summary>
    public string? SelectedChuteCode { get; set; }

    /// <summary>
    /// 表示统计结果的排序方式。
    /// </summary>
    public string SortOrder { get; set; } = "Most";

    /// <summary>
    /// 表示当前结果包含的统计行列表。
    /// </summary>
    public IReadOnlyList<RecirculationSummaryRowResponse> Rows { get; set; } = [];
}

