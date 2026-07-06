namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义当前类型。
/// </summary>
public class WmsFeedbackOptions
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    public const string SectionName = "WmsFeedback";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string SplitSchema { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string SplitTable { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string FullCaseSchema { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string FullCaseTable { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string SplitBusinessKeyColumn { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string FullCaseBusinessKeyColumn { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string FeedbackStatusColumn { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string FeedbackCompletedValue { get; set; } = "Y";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? FeedbackTimeColumn { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? ActualChuteColumn { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? ScanTimeColumn { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? LengthColumn { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? WidthColumn { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? HeightColumn { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? VolumeColumn { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? WeightColumn { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? ScanCountColumn { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? BusinessStatusColumn { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 300;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int BatchSize { get; set; } = 100;
}

