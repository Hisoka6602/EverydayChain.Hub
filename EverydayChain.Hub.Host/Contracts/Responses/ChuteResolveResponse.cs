namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class ChuteResolveResponse {
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool IsResolved { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string ChuteCode { get; set; } = string.Empty;
}

