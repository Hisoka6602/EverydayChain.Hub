namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义当前类型。
/// </summary>
public class ExceptionRuleOptions
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    public const string SectionName = "ExceptionRule";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool DryRun { get; set; }

    public WaveCleanupRuleOptions WaveCleanup { get; set; } = new();

    public MultiLabelRuleOptions MultiLabel { get; set; } = new();

    public RecirculationRuleOptions Recirculation { get; set; } = new();
}

