namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义 SyncJobOptions 类型。
/// </summary>
public class SyncJobOptions
{
    /// <summary>
    /// 存储 SectionName 字段。
    /// </summary>
    public const string SectionName = "SyncJob";

    /// <summary>
    /// 获取或设置 PollingIntervalSeconds。
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// 获取或设置 DefaultMaxLagMinutes。
    /// </summary>
    public int DefaultMaxLagMinutes { get; set; } = 10;

    /// <summary>
    /// 获取或设置 MaxParallelTables。
    /// </summary>
    public int MaxParallelTables { get; set; } = 1;

    /// <summary>
    /// 获取或设置 CheckpointFilePath。
    /// </summary>
    public string CheckpointFilePath { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 StartupMinFreeSpaceMb。
    /// </summary>
    public long StartupMinFreeSpaceMb { get; set; } = 500;

    /// <summary>
    /// 获取或设置 WriteMinFreeSpaceMb。
    /// </summary>
    public long WriteMinFreeSpaceMb { get; set; } = 100;

    /// <summary>
    /// 获取或设置 WriteSpaceCheckCacheSeconds。
    /// </summary>
    public int WriteSpaceCheckCacheSeconds { get; set; } = 5;

    /// <summary>
    /// 获取或设置 EnableTableMemoryMonitoring。
    /// </summary>
    public bool EnableTableMemoryMonitoring { get; set; } = true;

    /// <summary>
    /// 获取或设置 TableMemoryWarningThresholdMb。
    /// </summary>
    public long TableMemoryWarningThresholdMb { get; set; } = 256;

    /// <summary>
    /// 获取或设置 TableMemoryWarningLogIntervalSeconds。
    /// </summary>
    public int TableMemoryWarningLogIntervalSeconds { get; set; } = 300;

    /// <summary>
    /// 获取或设置 TableSyncTimeoutSeconds。
    /// </summary>
    public int TableSyncTimeoutSeconds { get; set; } = 600;

    /// <summary>
    /// 获取或设置 TableLeaseSeconds。
    /// </summary>
    public int TableLeaseSeconds { get; set; } = 900;

    /// <summary>
    /// 获取或设置 WatchdogTimeoutSeconds。
    /// </summary>
    public int WatchdogTimeoutSeconds { get; set; } = 1800;

    /// <summary>
    /// 获取或设置 EnableSetBasedMerge。
    /// </summary>
    public bool EnableSetBasedMerge { get; set; } = true;

    /// <summary>
    /// 获取或设置 BatchMergeSize。
    /// </summary>
    public int BatchMergeSize { get; set; } = 500;

    /// <summary>
    /// 获取或设置 Tables。
    /// </summary>
    public List<SyncTableOptions> Tables { get; set; } = [];
}

