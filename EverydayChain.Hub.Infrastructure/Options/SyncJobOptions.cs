namespace EverydayChain.Hub.Infrastructure.Options;

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

    /// <summary>单表配置集合。</summary>
    public List<SyncTableOptions> Tables { get; set; } = [];
}
