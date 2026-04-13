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
}
