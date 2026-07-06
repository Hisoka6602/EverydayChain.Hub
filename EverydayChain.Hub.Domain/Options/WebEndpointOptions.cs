namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class WebEndpointOptions
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    public const string SectionName = "WebEndpoint";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string Url { get; set; } = "http://localhost:5188";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;
}

