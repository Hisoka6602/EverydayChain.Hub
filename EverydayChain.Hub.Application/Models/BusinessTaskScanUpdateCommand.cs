namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 BusinessTaskScanUpdateCommand 类型。
/// </summary>
public sealed class BusinessTaskScanUpdateCommand
{
    /// <summary>
    /// 获取或设置 DeviceCode。
    /// </summary>
    public string? DeviceCode { get; set; }

    /// <summary>
    /// 获取或设置 TraceId。
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// 获取或设置 Barcode。
    /// </summary>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 TargetChuteCode。
    /// </summary>
    public string? TargetChuteCode { get; set; }

    /// <summary>
    /// 获取或设置 ScanTimeLocal。
    /// </summary>
    public DateTime ScanTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 UpdatedTimeLocal。
    /// </summary>
    public DateTime UpdatedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 LengthMm。
    /// </summary>
    public decimal? LengthMm { get; set; }

    /// <summary>
    /// 获取或设置 WidthMm。
    /// </summary>
    public decimal? WidthMm { get; set; }

    /// <summary>
    /// 获取或设置 HeightMm。
    /// </summary>
    public decimal? HeightMm { get; set; }

    /// <summary>
    /// 获取或设置 VolumeMm3。
    /// </summary>
    public decimal? VolumeMm3 { get; set; }

    /// <summary>
    /// 获取或设置 WeightGram。
    /// </summary>
    public decimal? WeightGram { get; set; }
}

