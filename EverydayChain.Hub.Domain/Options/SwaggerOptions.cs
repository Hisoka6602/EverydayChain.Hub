namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class SwaggerOptions {
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    public const string SectionName = "Swagger";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string Title { get; set; } = "EverydayChain Hub 对外接口";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string Version { get; set; } = "v1";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string Description { get; set; } = "提供扫描上传、请求格口、落格回传、波次清理、总看板查询、码头看板查询、分拣报表查询与导出，以及业务任务/异常件/回流记录查询等对外 API 能力。";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string Path { get; set; } = "/swagger";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool EnableInDevelopment { get; set; } = true;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool EnableInTest { get; set; } = true;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool EnableInProduction { get; set; } = false;
}

