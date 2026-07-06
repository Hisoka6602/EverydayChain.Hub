namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class ScanUploadApplicationRequest {
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string DeviceCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime ScanTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public decimal? LengthMm { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public decimal? WidthMm { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public decimal? HeightMm { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public decimal? VolumeMm3 { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public decimal? WeightGram { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? TargetChuteCode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? BarcodeType { get; set; }
}

