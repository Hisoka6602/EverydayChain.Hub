namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class DropFeedbackResponse {
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
    public string Status { get; set; } = string.Empty;
}

