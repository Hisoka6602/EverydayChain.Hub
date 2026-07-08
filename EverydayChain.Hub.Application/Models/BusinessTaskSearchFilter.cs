namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 BusinessTaskSearchFilter 类型。
/// </summary>
public sealed class BusinessTaskSearchFilter
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
    /// 获取或设置 OnlyException。
    /// </summary>
    public bool OnlyException { get; set; }

    /// <summary>
    /// 获取或设置 OnlyRecirculation。
    /// </summary>
    public bool OnlyRecirculation { get; set; }
}

