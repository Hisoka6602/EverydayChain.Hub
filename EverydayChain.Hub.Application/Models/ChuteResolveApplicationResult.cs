namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 请求格口应用层执行结果。
/// </summary>
public sealed class ChuteResolveApplicationResult {
    /// <summary>
    /// 是否成功解析格口。
    /// </summary>
    public bool IsResolved { get; set; }

    /// <summary>
    /// 业务任务编码。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 目标格口编码。
    /// </summary>
    public string ChuteCode { get; set; } = string.Empty;

    /// <summary>
    /// 结果描述。
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
