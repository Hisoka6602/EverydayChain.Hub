namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义 WebEndpointOptions 类型。
/// </summary>
public sealed class WebEndpointOptions
{
    /// <summary>
    /// 存储 SectionName 字段。
    /// </summary>
    public const string SectionName = "WebEndpoint";

    /// <summary>
    /// 获取或设置 Url。
    /// </summary>
    public string Url { get; set; } = "http://localhost:5188";

    /// <summary>
    /// 获取或设置 RequestTimeoutSeconds。
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;
}

