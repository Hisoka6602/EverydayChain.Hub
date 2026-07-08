namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义 RetentionJobOptions 类型。
/// </summary>
public class RetentionJobOptions
{
    /// <summary>
    /// 存储 SectionName 字段。
    /// </summary>
    public const string SectionName = "RetentionJob";

    /// <summary>
    /// 获取或设置 Enabled。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 获取或设置 PollingIntervalSeconds。
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 3600;

    /// <summary>
    /// 获取或设置 AllowDangerousAction。
    /// </summary>
    public bool AllowDangerousAction { get; set; }

    /// <summary>
    /// 获取或设置 LogTables。
    /// </summary>
    public List<RetentionLogTableOptions> LogTables { get; set; } = [];
}

