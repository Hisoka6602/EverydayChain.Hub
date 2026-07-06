namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class BusinessTaskQueryResponse
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public IReadOnlyList<BusinessTaskItemResponse> Items { get; set; } = [];

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool HasMore { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime? NextLastCreatedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public long? NextLastId { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string PaginationMode { get; set; } = "PageNumber";
}

