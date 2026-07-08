namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义 EfCoreOptions 类型。
/// </summary>
public sealed class EfCoreOptions
{
    /// <summary>
    /// 存储 SectionName 字段。
    /// </summary>
    public const string SectionName = "EfCore";

    /// <summary>
    /// 获取或设置 CommandTimeoutSeconds。
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;
}

