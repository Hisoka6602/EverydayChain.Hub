namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class ScanUploadApplicationResult {
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool IsAccepted { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string BarcodeType { get; set; } = "Unknown";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string FailureReason { get; set; } = string.Empty;
}

