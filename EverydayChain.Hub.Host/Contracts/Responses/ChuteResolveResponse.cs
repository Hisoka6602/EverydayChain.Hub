namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 请求格口返回体。
/// </summary>
public sealed class ChuteResolveResponse {
    /// <summary>
    /// 是否解析到目标格口。
    /// true 表示已匹配到格口；false 表示未匹配到有效格口。
    /// </summary>
    public bool IsResolved { get; set; }

    /// <summary>
    /// 业务任务编码。
    /// 成功场景返回匹配任务编码；未命中场景可能为空字符串。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 目标格口编码。
    /// 成功场景返回可执行落格的格口号；失败场景可能为空字符串。
    /// </summary>
    public string ChuteCode { get; set; } = string.Empty;
}
