namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示单个条码的扫描受理结果。
/// </summary>
public sealed class ScanUploadResponse {
    /// <summary>
    /// 表示当前请求是否已经被系统受理。
    /// </summary>
    public bool IsAccepted { get; set; }

    /// <summary>
    /// 表示业务任务编码。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 表示当前条码识别出的条码类型。
    /// </summary>
    public string BarcodeType { get; set; } = "Unknown";

    /// <summary>
    /// 表示处理失败、未命中或被拒绝的原因说明。
    /// </summary>
    public string FailureReason { get; set; } = string.Empty;
}

