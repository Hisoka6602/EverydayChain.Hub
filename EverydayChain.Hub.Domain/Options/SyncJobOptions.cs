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

    /// <summary>目标端同步数据落地文件路径（为空时使用应用基目录下 data/sync-target-store.json）。</summary>
    public string TargetStoreFilePath { get; set; } = string.Empty;

    /// <summary>启动自检最小可用磁盘空间（MB，建议范围：0~10240；0 表示关闭启动阶段磁盘空间校验）。</summary>
    public long StartupMinFreeSpaceMb { get; set; } = 500;

    /// <summary>关键写入最小可用磁盘空间（MB，建议范围：0~10240；0 表示关闭写入前磁盘空间校验）。</summary>
    public long WriteMinFreeSpaceMb { get; set; } = 100;

    /// <summary>关键写入磁盘空间检查缓存秒数（建议范围：0~300，0 表示每次都检查）。</summary>
    public int WriteSpaceCheckCacheSeconds { get; set; } = 5;

    /// <summary>单表配置集合。</summary>
    public List<SyncTableOptions> Tables { get; set; } = [];
}
