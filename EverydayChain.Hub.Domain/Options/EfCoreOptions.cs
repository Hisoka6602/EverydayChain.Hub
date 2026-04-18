namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// EF Core 运行时配置，从 <c>appsettings.json</c> 的 <c>EfCore</c> 节点绑定。
/// </summary>
public sealed class EfCoreOptions
{
    /// <summary>配置节名称。</summary>
    public const string SectionName = "EfCore";

    /// <summary>
    /// DbContext 池大小（可填写范围：32~1024；默认 256）。
    /// </summary>
    public int DbContextPoolSize { get; set; } = 256;

    /// <summary>
    /// SQL 命令超时秒数（可填写范围：1~600；默认 30）。
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;
}
