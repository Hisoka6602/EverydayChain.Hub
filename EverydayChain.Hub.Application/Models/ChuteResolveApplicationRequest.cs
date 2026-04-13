namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 请求格口应用层请求模型。
/// </summary>
public sealed class ChuteResolveApplicationRequest {
    /// <summary>
    /// 业务任务编码。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 条码文本。
    /// </summary>
    public string Barcode { get; set; } = string.Empty;
}
