using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class ScanUploadRequest {
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public List<string>? Barcodes { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string DeviceCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime ScanTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? LengthMm { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? WidthMm { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? HeightMm { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? VolumeMm3 { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? WeightGram { get; set; }
}

