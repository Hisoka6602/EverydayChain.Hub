namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义当前类型。
/// </summary>
public class FeedbackCompensationJobOptions
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    public const string SectionName = "FeedbackCompensationJob";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 300;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int BatchSize { get; set; } = 100;
}

