namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 扫描上传应用层执行结果。
/// </summary>
public sealed class ScanUploadApplicationResult {
    /// <summary>
    /// 是否处理成功。
    /// </summary>
    public bool IsAccepted { get; set; }

    /// <summary>
    /// 业务任务编码。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 结果描述。
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
