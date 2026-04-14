using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 扫描上传请求。
/// </summary>
public sealed class ScanUploadRequest {
    /// <summary>
    /// 单条条码文本（兼容字段），长度范围 1~128。
    /// </summary>
    [MaxLength(128)]
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// 多条条码文本列表（推荐字段），每项长度范围 1~128。
    /// </summary>
    public List<string> Barcodes { get; set; } = [];

    /// <summary>
    /// 设备编码，长度范围 1~64。
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string DeviceCode { get; set; } = string.Empty;

    /// <summary>
    /// 扫描时间（本地时间）。
    /// </summary>
    public DateTime ScanTimeLocal { get; set; }

    /// <summary>
    /// 链路追踪标识，长度范围 0~64。
    /// </summary>
    [MaxLength(64)]
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// 包裹长度，单位毫米，范围 >= 0。
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? LengthMm { get; set; }

    /// <summary>
    /// 包裹宽度，单位毫米，范围 >= 0。
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? WidthMm { get; set; }

    /// <summary>
    /// 包裹高度，单位毫米，范围 >= 0。
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? HeightMm { get; set; }

    /// <summary>
    /// 包裹体积，单位立方毫米，范围 >= 0。
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? VolumeMm3 { get; set; }

    /// <summary>
    /// 包裹重量，单位克，范围 >= 0。
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? WeightGram { get; set; }
}
