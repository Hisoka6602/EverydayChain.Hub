namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 表示单张日志表或固定表的保留期治理配置。
/// 该配置同时支持“删除旧分表”和“按时间列删除旧数据行”两种治理模式。
/// </summary>
public class RetentionLogTableOptions
{
    /// <summary>
    /// 获取或设置是否启用该表的保留期治理。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 获取或设置逻辑表名或固定表名。
    /// </summary>
    public string LogicalTableName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置保留模式。
    /// 可选值为 DropShards 或 DeleteRows；未填写时默认使用 DropShards。
    /// </summary>
    public string RetentionMode { get; set; } = "DropShards";

    /// <summary>
    /// 获取或设置固定表删行模式使用的时间列名。
    /// 当 RetentionMode=DeleteRows 时必须填写；其他模式可留空。
    /// </summary>
    public string TimeColumnName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置保留月数。
    /// 超过该保留窗口的旧分表或旧数据行将成为治理候选。
    /// </summary>
    public int KeepMonths { get; set; } = 3;

    /// <summary>
    /// 获取或设置是否仅做预演而不执行实际删除。
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// 获取或设置是否允许执行危险删除动作。
    /// 对于 DropShards 模式表示允许删旧分表；对于 DeleteRows 模式表示允许删旧数据行。
    /// </summary>
    public bool AllowDrop { get; set; }

    /// <summary>
    /// 获取或设置固定表删行模式的单批删除行数。
    /// 该值仅在 RetentionMode=DeleteRows 时生效。
    /// </summary>
    public int DeleteBatchSize { get; set; } = 10000;
}
