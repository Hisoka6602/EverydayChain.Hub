namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncJobOptions
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    public const string SectionName = "SyncJob";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int DefaultMaxLagMinutes { get; set; } = 10;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int MaxParallelTables { get; set; } = 1;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string CheckpointFilePath { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public long StartupMinFreeSpaceMb { get; set; } = 500;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public long WriteMinFreeSpaceMb { get; set; } = 100;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int WriteSpaceCheckCacheSeconds { get; set; } = 5;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool EnableTableMemoryMonitoring { get; set; } = true;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public long TableMemoryWarningThresholdMb { get; set; } = 256;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int TableMemoryWarningLogIntervalSeconds { get; set; } = 300;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int TableSyncTimeoutSeconds { get; set; } = 600;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int TableLeaseSeconds { get; set; } = 900;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int WatchdogTimeoutSeconds { get; set; } = 1800;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool EnableSetBasedMerge { get; set; } = true;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int BatchMergeSize { get; set; } = 500;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public List<SyncTableOptions> Tables { get; set; } = [];
}

