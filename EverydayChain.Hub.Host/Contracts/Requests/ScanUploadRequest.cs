using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 表示扫描上传请求参数。
/// </summary>
public sealed class ScanUploadRequest {
    /// <summary>
    /// 表示本次请求提交的条码列表。
    /// </summary>
    public List<string>? Barcodes { get; set; }

    /// <summary>
    /// 表示发起本次扫描上传的设备编码。
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string DeviceCode { get; set; } = string.Empty;

    /// <summary>
    /// 表示扫描发生时间（本地时间）。
    /// </summary>
    public DateTime ScanTimeLocal { get; set; }

    /// <summary>
    /// 表示本次扫描请求的链路跟踪标识。
    /// </summary>
    [MaxLength(64)]
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// 表示箱体长度，单位为毫米。
    /// </summary>
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal? LengthMm { get; set; }

    /// <summary>
    /// 表示箱体宽度，单位为毫米。
    /// </summary>
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal? WidthMm { get; set; }

    /// <summary>
    /// 表示箱体高度，单位为毫米。
    /// </summary>
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal? HeightMm { get; set; }

    /// <summary>
    /// 表示箱体体积，单位为立方毫米。
    /// </summary>
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal? VolumeMm3 { get; set; }

    /// <summary>
    /// 表示箱体重量，单位为克。
    /// </summary>
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal? WeightGram { get; set; }
}

