namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 ExportCatalogQueryResult 类型。
/// </summary>
public sealed class ExportCatalogQueryResult
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
    /// 获取或设置 GeneratedTimeLocal。
    /// </summary>
    public DateTime GeneratedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 Items。
    /// </summary>
    public IReadOnlyList<ExportCatalogItem> Items { get; set; } = [];
}

