namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class BusinessTaskQueryRequest
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
    public string? WaveCode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? DockCode { get; set; }

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

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime? LastCreatedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public long? LastId { get; set; }
}

