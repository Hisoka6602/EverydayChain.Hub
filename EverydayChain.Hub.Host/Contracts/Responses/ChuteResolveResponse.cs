namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 请求格口返回体。
/// </summary>
public sealed class ChuteResolveResponse {
    /// <summary>
    /// 是否解析成功。
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
}
