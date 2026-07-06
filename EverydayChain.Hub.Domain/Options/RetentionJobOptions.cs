namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义当前类型。
/// </summary>
public class RetentionJobOptions
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    public const string SectionName = "RetentionJob";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 3600;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool AllowDangerousAction { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public List<RetentionLogTableOptions> LogTables { get; set; } = [];
}

