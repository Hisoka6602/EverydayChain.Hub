namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// Web 监听地址配置。
/// </summary>
public sealed class WebEndpointOptions
{
    /// <summary>
    /// 配置节名称。
    /// </summary>
    public const string SectionName = "WebEndpoint";

    /// <summary>
    /// Web 监听地址（可填写范围：合法 http/https URL；默认值：http://localhost:5188）。
    /// </summary>
    public string Url { get; set; } = "http://localhost:5188";
}
