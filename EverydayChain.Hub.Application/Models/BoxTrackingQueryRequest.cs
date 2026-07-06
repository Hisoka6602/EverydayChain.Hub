namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class BoxTrackingQueryRequest
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
    public string? BoxId { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? OrderId { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? StoreId { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? Scanner { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? ChuteCode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int PageSize { get; set; } = 50;
}

