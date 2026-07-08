namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义 FeedbackCompensationJobOptions 类型。
/// </summary>
public class FeedbackCompensationJobOptions
{
    /// <summary>
    /// 存储 SectionName 字段。
    /// </summary>
    public const string SectionName = "FeedbackCompensationJob";

    /// <summary>
    /// 获取或设置 Enabled。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 获取或设置 PollingIntervalSeconds。
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 300;

    /// <summary>
    /// 获取或设置 BatchSize。
    /// </summary>
    public int BatchSize { get; set; } = 100;
}

