namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class BoxTrackingItemResponse
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string BoxId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? TaskCode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? WaveCode { get; set; }

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
    public string? Scanner { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime ScannedAt { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? Chute { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool IsMatched { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? FailureReason { get; set; }
}

