using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 扫描上传请求。
/// </summary>
public sealed class ScanUploadRequest {
    /// <summary>
    /// 条码文本，长度范围 1~128。
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string Barcode { get; set; } = string.Empty;

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
}
