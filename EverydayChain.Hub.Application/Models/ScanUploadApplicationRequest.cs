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
}
