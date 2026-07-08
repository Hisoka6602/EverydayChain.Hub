namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义 SwaggerOptions 类型。
/// </summary>
public sealed class SwaggerOptions {
    /// <summary>
    /// 存储 SectionName 字段。
    /// </summary>
    public const string SectionName = "Swagger";

    /// <summary>
    /// 获取或设置 Title。
    /// </summary>
    public string Title { get; set; } = "EverydayChain Hub 对外接口";

    /// <summary>
    /// 获取或设置 Version。
    /// </summary>
    public string Version { get; set; } = "v1";

    /// <summary>
    /// 获取或设置 Description。
    /// </summary>
    public string Description { get; set; } = "提供扫描上传、请求格口、落格回传、波次清理、总看板查询、码头看板查询、分拣报表查询与导出，以及业务任务/异常件/回流记录查询等对外 API 能力。";

    /// <summary>
    /// 获取或设置 Path。
    /// </summary>
    public string Path { get; set; } = "/swagger";

    /// <summary>
    /// 获取或设置 EnableInDevelopment。
    /// </summary>
    public bool EnableInDevelopment { get; set; } = true;

    /// <summary>
    /// 获取或设置 EnableInTest。
    /// </summary>
    public bool EnableInTest { get; set; } = true;

    /// <summary>
    /// 获取或设置 EnableInProduction。
    /// </summary>
    public bool EnableInProduction { get; set; } = false;
}

