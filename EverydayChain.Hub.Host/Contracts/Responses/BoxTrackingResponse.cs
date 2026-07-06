namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class BoxTrackingResponse
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
    public IReadOnlyList<BoxTrackingItemResponse> Items { get; set; } = [];
}

