namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义 SyncTableOptions 类型。
/// </summary>
public class SyncTableOptions
{
    /// <summary>
    /// 获取或设置 TableCode。
    /// </summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Enabled。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 获取或设置 SourceSchema。
    /// </summary>
    public string SourceSchema { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 SourceTable。
    /// </summary>
    public string SourceTable { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 TargetLogicalTable。
    /// </summary>
    public string TargetLogicalTable { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 CursorColumn。
    /// </summary>
    public string CursorColumn { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 StartTimeLocal。
    /// </summary>
    public string StartTimeLocal { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 PageSize。
    /// </summary>
    public int PageSize { get; set; } = 5000;

    /// <summary>
    /// 获取或设置 PollingIntervalSeconds。
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// 获取或设置 MaxLagMinutes。
    /// </summary>
    public int MaxLagMinutes { get; set; } = 10;

    /// <summary>
    /// 获取或设置 Priority。
    /// </summary>
    public string Priority { get; set; } = "Low";

    /// <summary>
    /// 获取或设置 UniqueKeys。
    /// </summary>
    public List<string> UniqueKeys { get; set; } = [];

    /// <summary>
    /// 获取或设置 ExcludedColumns。
    /// </summary>
    public List<string> ExcludedColumns { get; set; } = [];

    public SyncDeleteOptions Delete { get; set; } = new();

    public SyncRetentionOptions Retention { get; set; } = new();

    /// <summary>
    /// 获取或设置 SyncMode。
    /// </summary>
    public string SyncMode { get; set; } = "KeyedMerge";

    /// <summary>
    /// 获取或设置 StatusColumnName。
    /// </summary>
    public string StatusColumnName { get; set; } = "TASKPROCESS";

    /// <summary>
    /// 获取或设置 PendingStatusValue。
    /// </summary>
    public string? PendingStatusValue { get; set; } = "N";

    /// <summary>
    /// 获取或设置 CompletedStatusValue。
    /// </summary>
    public string CompletedStatusValue { get; set; } = "Y";

    /// <summary>
    /// 获取或设置 ShouldWriteBackRemoteStatus。
    /// </summary>
    public bool ShouldWriteBackRemoteStatus { get; set; } = true;

    /// <summary>
    /// 获取或设置 StatusBatchSize。
    /// </summary>
    public int StatusBatchSize { get; set; } = 5000;

    /// <summary>
    /// 获取或设置 WriteBackCompletedTimeColumnName。
    /// </summary>
    public string? WriteBackCompletedTimeColumnName { get; set; }

    /// <summary>
    /// 获取或设置 WriteBackBatchIdColumnName。
    /// </summary>
    public string? WriteBackBatchIdColumnName { get; set; }

    /// <summary>
    /// 获取或设置 SourceType。
    /// </summary>
    public string SourceType { get; set; } = "Unknown";

    /// <summary>
    /// 获取或设置 BusinessKeyColumn。
    /// </summary>
    public string BusinessKeyColumn { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 BarcodeColumn。
    /// </summary>
    public string? BarcodeColumn { get; set; }

    /// <summary>
    /// 获取或设置 WaveCodeColumn。
    /// </summary>
    public string? WaveCodeColumn { get; set; }

    /// <summary>
    /// 获取或设置 WaveRemarkColumn。
    /// </summary>
    public string? WaveRemarkColumn { get; set; }

    /// <summary>
    /// 获取或设置 WorkingAreaColumn。
    /// </summary>
    public string? WorkingAreaColumn { get; set; }

    /// <summary>
    /// 获取或设置 OrderIdColumn。
    /// </summary>
    public string? OrderIdColumn { get; set; }

    /// <summary>
    /// 获取或设置 StoreIdColumn。
    /// </summary>
    public string? StoreIdColumn { get; set; }

    /// <summary>
    /// 获取或设置 StoreNameColumn。
    /// </summary>
    public string? StoreNameColumn { get; set; }

    /// <summary>
    /// 获取或设置 ProductCodeColumn。
    /// </summary>
    public string? ProductCodeColumn { get; set; }

    /// <summary>
    /// 获取或设置 PickLocationColumn。
    /// </summary>
    public string? PickLocationColumn { get; set; }
}

