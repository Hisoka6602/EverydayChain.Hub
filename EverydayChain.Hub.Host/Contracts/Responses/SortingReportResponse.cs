namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class SortingReportResponse
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? SelectedDockCode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public IReadOnlyList<SortingReportRowResponse> Rows { get; set; } = [];
}

