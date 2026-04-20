namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 扫描上传应用层请求模型。
/// </summary>
public sealed class ScanUploadApplicationRequest {
    /// <summary>
    /// 条码文本。
    /// </summary>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// 设备编码。
    /// </summary>
    public string DeviceCode { get; set; } = string.Empty;

    /// <summary>
    /// 扫描时间（本地时间）。
    /// </summary>
    public DateTime ScanTimeLocal { get; set; }

    /// <summary>
    /// 链路追踪标识，长度范围 0~64。
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// 包裹长度，单位毫米，范围 >= 0。
    /// </summary>
    public decimal? LengthMm { get; set; }

    /// <summary>
    /// 包裹宽度，单位毫米，范围 >= 0。
    /// </summary>
    public decimal? WidthMm { get; set; }

    /// <summary>
    /// 包裹高度，单位毫米，范围 >= 0。
    /// </summary>
    public decimal? HeightMm { get; set; }

    /// <summary>
    /// 包裹体积，单位立方毫米，范围 >= 0。
    /// </summary>
    public decimal? VolumeMm3 { get; set; }

    /// <summary>
    /// 包裹重量，单位克，范围 >= 0。
    /// </summary>
    public decimal? WeightGram { get; set; }

    /// <summary>
    /// 条码解析得到的目标格口编码，长度范围 1~64。
    /// </summary>
    public string? TargetChuteCode { get; set; }

    /// <summary>
    /// 条码解析得到的条码类型文本，长度范围 0~32；可填写范围：Unknown、Split、FullCase。
    /// </summary>
    public string BarcodeType { get; set; } = string.Empty;
}
