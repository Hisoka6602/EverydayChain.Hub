namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 SortingReportQueryResult 类型。
/// </summary>
public sealed class SortingReportQueryResult
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
    /// 获取或设置 SelectedDockCode。
    /// </summary>
    public string? SelectedDockCode { get; set; }

    /// <summary>
    /// 获取或设置 Rows。
    /// </summary>
    public IReadOnlyList<SortingReportRow> Rows { get; set; } = [];
}

