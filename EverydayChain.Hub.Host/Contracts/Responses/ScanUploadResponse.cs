namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 扫描上传返回体。
/// </summary>
public sealed class ScanUploadResponse {
    /// <summary>
    /// 当前条码是否受理成功。
    /// true 表示条码进入后续业务链路；false 表示该条码被拒绝。
    /// </summary>
    public bool IsAccepted { get; set; }

    /// <summary>
    /// 业务任务编码。
    /// 受理成功时返回生成或匹配到的任务编码；失败时可能为空字符串。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 条码类型语义码。
    /// 可返回值：Split（拆零）、FullCase（整件）、Unknown（未知）。
    /// 失败场景下通常返回 Unknown。
    /// </summary>
    public string BarcodeType { get; set; } = "Unknown";

    /// <summary>
    /// 失败语义代码。
    /// 可返回值：InvalidBarcode（无效条码）、UnsupportedBarcodeType（不支持的条码类型）、ParseError（解析异常）；成功时为空字符串。
    /// </summary>
    public string FailureReason { get; set; } = string.Empty;
}
