namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class RecirculationSummaryQueryResult
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
    public string? SelectedChuteCode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string SortOrder { get; set; } = "Most";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public IReadOnlyList<RecirculationSummaryRow> Rows { get; set; } = [];
}

