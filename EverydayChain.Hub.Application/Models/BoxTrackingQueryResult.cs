namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 BoxTrackingQueryResult 类型。
/// </summary>
public sealed class BoxTrackingQueryResult
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
    /// 获取或设置 TotalCount。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 获取或设置 PageNumber。
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// 获取或设置 PageSize。
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 获取或设置 Items。
    /// </summary>
    public IReadOnlyList<BoxTrackingItem> Items { get; set; } = [];
}

