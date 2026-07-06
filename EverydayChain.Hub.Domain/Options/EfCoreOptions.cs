namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class EfCoreOptions
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    public const string SectionName = "EfCore";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;
}

