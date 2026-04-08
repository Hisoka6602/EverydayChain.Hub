namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 同步任务全局配置。
/// </summary>
public class SyncJobOptions
{
    /// <summary>配置节名称。</summary>
    public const string SectionName = "SyncJob";

    /// <summary>全局轮询间隔（秒）。</summary>
    public int PollingIntervalSeconds { get; set; } = 60;

    /// <summary>全局默认最大滞后分钟数。</summary>
    public int DefaultMaxLagMinutes { get; set; } = 10;

    /// <summary>全局多表并发上限（最小 1），用于限制同轮同步并行表数量。</summary>
    public int MaxParallelTables { get; set; } = 1;

    /// <summary>检查点文件路径（为空时使用应用基目录下 sync-checkpoints.json）。</summary>
    public string CheckpointFilePath { get; set; } = string.Empty;

    /// <summary>启动自检最小可用磁盘空间（MB，建议范围：0~10240；0 表示关闭启动阶段磁盘空间校验）。</summary>
    public long StartupMinFreeSpaceMb { get; set; } = 500;

    /// <summary>关键写入最小可用磁盘空间（MB，建议范围：0~10240；0 表示关闭写入前磁盘空间校验）。</summary>
    public long WriteMinFreeSpaceMb { get; set; } = 100;

    /// <summary>关键写入磁盘空间检查缓存秒数（建议范围：0~300，0 表示每次都检查）。</summary>
    public int WriteSpaceCheckCacheSeconds { get; set; } = 5;

    /// <summary>是否启用单表内存水位监控（可填写项：true、false）。</summary>
    public bool EnableTableMemoryMonitoring { get; set; } = true;

    /// <summary>单表内存水位告警阈值（MB，可填写范围：[0, 65536] 的整数；0 表示关闭阈值告警；超过 65536 将钳制为 65536；建议值 256）。</summary>
    public long TableMemoryWarningThresholdMb { get; set; } = 256;

    /// <summary>单表内存告警节流间隔（秒，可填写范围：[0, 86400] 的整数；0 表示不节流；建议值 300）。</summary>
    public int TableMemoryWarningLogIntervalSeconds { get; set; } = 300;

    /// <summary>单轮同步整体超时时间（单位：秒，可填写范围：[0, 86400]；当前用于限制一次 RunAllEnabledTableSyncAsync 的整轮执行时长；0 表示关闭超时保护；建议值 600）。</summary>
    public int TableSyncTimeoutSeconds { get; set; } = 600;

    /// <summary>后台任务看门狗超时时间（单位：秒，可填写范围：[0, 86400]；0 表示关闭看门狗检测；建议值 1800）。主循环超过此阈值未推进时，输出 Critical 日志提示运维检查并重启服务。</summary>
    public int WatchdogTimeoutSeconds { get; set; } = 1800;

    /// <summary>是否启用按页集合式 MERGE（可填写项：true、false；建议值 true）。</summary>
    public bool EnableSetBasedMerge { get; set; } = true;

    /// <summary>按页集合式 MERGE 的批次大小（建议范围：1~5000；建议值 500）。</summary>
    public int BatchMergeSize { get; set; } = 500;

    /// <summary>单表配置集合。</summary>
    public List<SyncTableOptions> Tables { get; set; } = [];
}
