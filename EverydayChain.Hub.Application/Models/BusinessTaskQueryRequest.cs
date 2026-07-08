namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 BusinessTaskQueryRequest 类型。
/// </summary>
public sealed class BusinessTaskQueryRequest
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
    /// 获取或设置 WaveCode。
    /// </summary>
    public string? WaveCode { get; set; }

    /// <summary>
    /// 获取或设置 Barcode。
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 获取或设置 DockCode。
    /// </summary>
    public string? DockCode { get; set; }

    /// <summary>
    /// 获取或设置 ChuteCode。
    /// </summary>
    public string? ChuteCode { get; set; }

    /// <summary>
    /// 获取或设置 PageNumber。
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// 获取或设置 PageSize。
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// 获取或设置 LastCreatedTimeLocal。
    /// </summary>
    public DateTime? LastCreatedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 LastId。
    /// </summary>
    public long? LastId { get; set; }
}

