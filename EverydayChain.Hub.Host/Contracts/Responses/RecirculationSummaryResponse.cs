namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class RecirculationSummaryResponse
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
    public IReadOnlyList<RecirculationSummaryRowResponse> Rows { get; set; } = [];
}

