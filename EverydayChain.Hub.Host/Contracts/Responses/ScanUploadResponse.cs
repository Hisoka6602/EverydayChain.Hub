namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 扫描上传返回体。
/// </summary>
public sealed class ScanUploadResponse {
    /// <summary>
    /// 是否受理成功。
    /// </summary>
    public bool IsAccepted { get; set; }

    /// <summary>
    /// 业务任务编码。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 条码类型，候选值：Split、FullCase、Unknown。
    /// </summary>
    public string BarcodeType { get; set; } = "Unknown";

    /// <summary>
    /// 失败语义代码，候选值：InvalidBarcode、UnsupportedBarcodeType、ParseError；成功时为空。
    /// </summary>
    public string FailureReason { get; set; } = string.Empty;
}
