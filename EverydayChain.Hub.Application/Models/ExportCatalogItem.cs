namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 ExportCatalogItem 类型。
/// </summary>
public sealed class ExportCatalogItem
{
    /// <summary>
    /// 获取或设置 Key。
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Scope。
    /// </summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Type。
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Content。
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Format。
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Endpoint。
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 UpdatedTimeLocal。
    /// </summary>
    public DateTime UpdatedTimeLocal { get; set; }
}

