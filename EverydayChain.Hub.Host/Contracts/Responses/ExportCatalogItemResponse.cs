namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示导出中心中的单个导出项。
/// </summary>
public sealed class ExportCatalogItemResponse
{
    /// <summary>
    /// 表示导出项唯一标识。
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 表示导出项所属业务范围。
    /// </summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>
    /// 表示导出项类型。
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 表示导出项展示名称或内容摘要。
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 表示导出文件格式。
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// 表示对应的导出接口路径。
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// 表示记录最后更新时间（本地时间）。
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

