namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 SortingReportQueryRequest 类型。
/// </summary>
public sealed class SortingReportQueryRequest
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
    /// 获取或设置 DockCode。
    /// </summary>
    public string? DockCode { get; set; }
}

