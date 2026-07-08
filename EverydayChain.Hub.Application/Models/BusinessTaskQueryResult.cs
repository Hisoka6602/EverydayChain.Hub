namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 BusinessTaskQueryResult 类型。
/// </summary>
public sealed class BusinessTaskQueryResult
{
    /// <summary>
    /// 获取或设置 TotalCount。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 获取或设置 PageNumber。
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// 获取或设置 PageSize。
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 获取或设置 Items。
    /// </summary>
    public IReadOnlyList<BusinessTaskQueryItem> Items { get; set; } = [];

    /// <summary>
    /// 获取或设置 HasMore。
    /// </summary>
    public bool HasMore { get; set; }

    /// <summary>
    /// 获取或设置 NextLastCreatedTimeLocal。
    /// </summary>
    public DateTime? NextLastCreatedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 NextLastId。
    /// </summary>
    public long? NextLastId { get; set; }

    /// <summary>
    /// 获取或设置 PaginationMode。
    /// </summary>
    public string PaginationMode { get; set; } = "PageNumber";
}

