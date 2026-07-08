namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示导出中心目录查询结果。
/// </summary>
public sealed class ExportCatalogResponse
{
    /// <summary>
    /// 表示查询或统计开始时间（本地时间）。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 表示查询或统计结束时间（本地时间）。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }

    /// <summary>
    /// 表示导出目录生成时间（本地时间）。
    /// </summary>
    public DateTime GeneratedTimeLocal { get; set; }

    /// <summary>
    /// 表示当前结果包含的明细列表。
    /// </summary>
    public IReadOnlyList<ExportCatalogItemResponse> Items { get; set; } = [];
}

