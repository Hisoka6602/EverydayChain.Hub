namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示格口解析结果。
/// </summary>
public sealed class ChuteResolveResponse {
    /// <summary>
    /// 表示当前条码是否成功解析出目标格口。
    /// </summary>
    public bool IsResolved { get; set; }

    /// <summary>
    /// 表示业务任务编码。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 表示格口编码。
    /// </summary>
    public string ChuteCode { get; set; } = string.Empty;
}

