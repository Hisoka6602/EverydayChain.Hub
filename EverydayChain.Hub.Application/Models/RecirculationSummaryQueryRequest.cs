namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 RecirculationSummaryQueryRequest 类型。
/// </summary>
public sealed class RecirculationSummaryQueryRequest
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
    /// 获取或设置 ChuteCode。
    /// </summary>
    public string? ChuteCode { get; set; }

    /// <summary>
    /// 获取或设置 SortOrder。
    /// </summary>
    public string SortOrder { get; set; } = "Most";
}

