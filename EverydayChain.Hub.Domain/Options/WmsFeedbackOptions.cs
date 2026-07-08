namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义 WmsFeedbackOptions 类型。
/// </summary>
public class WmsFeedbackOptions
{
    /// <summary>
    /// 存储 SectionName 字段。
    /// </summary>
    public const string SectionName = "WmsFeedback";

    /// <summary>
    /// 获取或设置 Enabled。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 获取或设置 SplitSchema。
    /// </summary>
    public string SplitSchema { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 SplitTable。
    /// </summary>
    public string SplitTable { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 FullCaseSchema。
    /// </summary>
    public string FullCaseSchema { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 FullCaseTable。
    /// </summary>
    public string FullCaseTable { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 SplitBusinessKeyColumn。
    /// </summary>
    public string SplitBusinessKeyColumn { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 FullCaseBusinessKeyColumn。
    /// </summary>
    public string FullCaseBusinessKeyColumn { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 FeedbackStatusColumn。
    /// </summary>
    public string FeedbackStatusColumn { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 FeedbackCompletedValue。
    /// </summary>
    public string FeedbackCompletedValue { get; set; } = "Y";

    /// <summary>
    /// 获取或设置 FeedbackTimeColumn。
    /// </summary>
    public string? FeedbackTimeColumn { get; set; }

    /// <summary>
    /// 获取或设置 ActualChuteColumn。
    /// </summary>
    public string? ActualChuteColumn { get; set; }

    /// <summary>
    /// 获取或设置 ScanTimeColumn。
    /// </summary>
    public string? ScanTimeColumn { get; set; }

    /// <summary>
    /// 获取或设置 LengthColumn。
    /// </summary>
    public string? LengthColumn { get; set; }

    /// <summary>
    /// 获取或设置 WidthColumn。
    /// </summary>
    public string? WidthColumn { get; set; }

    /// <summary>
    /// 获取或设置 HeightColumn。
    /// </summary>
    public string? HeightColumn { get; set; }

    /// <summary>
    /// 获取或设置 VolumeColumn。
    /// </summary>
    public string? VolumeColumn { get; set; }

    /// <summary>
    /// 获取或设置 WeightColumn。
    /// </summary>
    public string? WeightColumn { get; set; }

    /// <summary>
    /// 获取或设置 ScanCountColumn。
    /// </summary>
    public string? ScanCountColumn { get; set; }

    /// <summary>
    /// 获取或设置 BusinessStatusColumn。
    /// </summary>
    public string? BusinessStatusColumn { get; set; }

    /// <summary>
    /// 获取或设置 CommandTimeoutSeconds。
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// 获取或设置 PollingIntervalSeconds。
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 300;

    /// <summary>
    /// 获取或设置 BatchSize。
    /// </summary>
    public int BatchSize { get; set; } = 100;
}

