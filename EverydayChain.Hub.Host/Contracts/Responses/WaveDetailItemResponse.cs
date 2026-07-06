namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class WaveDetailItemResponse
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string WaveCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? WorkingArea { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? Barcode { get; set; }

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
    public string? StoreName { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? ProductCode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? PickLocation { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? ChuteCode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool IsRecirculated { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool IsException { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime? ScannedAt { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

