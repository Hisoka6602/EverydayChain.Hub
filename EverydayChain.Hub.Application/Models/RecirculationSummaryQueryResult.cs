namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 RecirculationSummaryQueryResult 类型。
/// </summary>
public sealed class RecirculationSummaryQueryResult
{
    /// <summary>
    /// 获取或设置 StartTimeLocal。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 EndTimeLocal。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 SelectedChuteCode。
    /// </summary>
    public string? SelectedChuteCode { get; set; }

    /// <summary>
    /// 获取或设置 SortOrder。
    /// </summary>
    public string SortOrder { get; set; } = "Most";

    /// <summary>
    /// 获取或设置 Rows。
    /// </summary>
    public IReadOnlyList<RecirculationSummaryRow> Rows { get; set; } = [];
}

