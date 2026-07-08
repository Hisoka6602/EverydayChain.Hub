namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 ChuteResolveApplicationResult 类型。
/// </summary>
public sealed class ChuteResolveApplicationResult {
    /// <summary>
    /// 获取或设置 IsResolved。
    /// </summary>
    public bool IsResolved { get; set; }

    /// <summary>
    /// 获取或设置 TaskCode。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 ChuteCode。
    /// </summary>
    public string ChuteCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Message。
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

