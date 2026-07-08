namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 表示导出中心目录查询条件。
/// </summary>
public sealed class ExportCatalogQueryRequest
{
    /// <summary>
    /// 表示查询或统计开始时间（本地时间）。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 表示查询或统计结束时间（本地时间）。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }
}

