namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义 ExceptionRuleOptions 类型。
/// </summary>
public class ExceptionRuleOptions
{
    /// <summary>
    /// 存储 SectionName 字段。
    /// </summary>
    public const string SectionName = "ExceptionRule";

    /// <summary>
    /// 获取或设置 Enabled。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 获取或设置 DryRun。
    /// </summary>
    public bool DryRun { get; set; }

    public WaveCleanupRuleOptions WaveCleanup { get; set; } = new();

    public MultiLabelRuleOptions MultiLabel { get; set; } = new();

    public RecirculationRuleOptions Recirculation { get; set; } = new();
}

