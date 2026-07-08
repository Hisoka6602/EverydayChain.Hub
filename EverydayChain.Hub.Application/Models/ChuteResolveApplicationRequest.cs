namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 ChuteResolveApplicationRequest 类型。
/// </summary>
public sealed class ChuteResolveApplicationRequest {
    /// <summary>
    /// 获取或设置 TaskCode。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Barcode。
    /// </summary>
    public string Barcode { get; set; } = string.Empty;
}

