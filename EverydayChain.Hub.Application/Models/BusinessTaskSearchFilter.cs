namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 业务任务查询过滤条件。
/// </summary>
public sealed class BusinessTaskSearchFilter
{
    /// <summary>
    /// 查询开始时间（本地时间，包含边界）。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 查询结束时间（本地时间，不包含边界）。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }

    /// <summary>
    /// 可选波次编码。
    /// </summary>
    public string? WaveCode { get; set; }

    /// <summary>
    /// 可选条码。
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 可选码头编码。
    /// </summary>
    public string? DockCode { get; set; }

    /// <summary>
    /// 可选格口编码（匹配目标格口或实际格口）。
    /// </summary>
    public string? ChuteCode { get; set; }

    /// <summary>
    /// 是否仅查询异常件。
    /// </summary>
    public bool OnlyException { get; set; }

    /// <summary>
    /// 是否仅查询回流件。
    /// </summary>
    public bool OnlyRecirculation { get; set; }
}
