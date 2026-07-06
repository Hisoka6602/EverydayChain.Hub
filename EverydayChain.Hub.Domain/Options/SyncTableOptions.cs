namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncTableOptions
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string SourceSchema { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string SourceTable { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string TargetLogicalTable { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string CursorColumn { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string StartTimeLocal { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int PageSize { get; set; } = 5000;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int MaxLagMinutes { get; set; } = 10;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string Priority { get; set; } = "Low";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public List<string> UniqueKeys { get; set; } = [];

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public List<string> ExcludedColumns { get; set; } = [];

    public SyncDeleteOptions Delete { get; set; } = new();

    public SyncRetentionOptions Retention { get; set; } = new();

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string SyncMode { get; set; } = "KeyedMerge";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string StatusColumnName { get; set; } = "TASKPROCESS";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? PendingStatusValue { get; set; } = "N";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string CompletedStatusValue { get; set; } = "Y";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool ShouldWriteBackRemoteStatus { get; set; } = true;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int StatusBatchSize { get; set; } = 5000;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? WriteBackCompletedTimeColumnName { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? WriteBackBatchIdColumnName { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string SourceType { get; set; } = "Unknown";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string BusinessKeyColumn { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? BarcodeColumn { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? WaveCodeColumn { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? WaveRemarkColumn { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? WorkingAreaColumn { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? OrderIdColumn { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? StoreIdColumn { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? StoreNameColumn { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? ProductCodeColumn { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? PickLocationColumn { get; set; }
}

