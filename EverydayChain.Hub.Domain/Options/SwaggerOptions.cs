namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// Swagger 文档配置。
/// </summary>
public sealed class SwaggerOptions {
    /// <summary>
    /// 配置节名称。
    /// </summary>
    public const string SectionName = "Swagger";

    /// <summary>
    /// 文档标题（可填写范围：长度 1~128 的文本）。
    /// </summary>
    public string Title { get; set; } = "EverydayChain Hub 对外接口";

    /// <summary>
    /// 文档版本（可填写范围：长度 1~32 的文本，例如 v1）。
    /// </summary>
    public string Version { get; set; } = "v1";

    /// <summary>
    /// 文档描述（可填写范围：长度 0~512 的文本）。
    /// </summary>
    public string Description { get; set; } = "提供扫描上传、请求格口、落格回传、波次清理、总看板查询、码头看板查询、分拣报表查询与导出、业务任务/异常件/回流记录查询等对外 API 能力。";

    /// <summary>
    /// Swagger 页面入口路径（可填写范围：以 / 开头的路径；默认值：/swagger）。
    /// </summary>
    public string Path { get; set; } = "/swagger";

    /// <summary>
    /// 开发环境是否启用 Swagger（可填写项：true、false）。
    /// </summary>
    public bool EnableInDevelopment { get; set; } = true;

    /// <summary>
    /// 测试环境是否启用 Swagger（可填写项：true、false）。
    /// </summary>
    public bool EnableInTest { get; set; } = true;

    /// <summary>
    /// 生产环境是否启用 Swagger（可填写项：true、false）。
    /// </summary>
    public bool EnableInProduction { get; set; } = false;
}
