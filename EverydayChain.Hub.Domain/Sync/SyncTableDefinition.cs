using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Sync.Models;

namespace EverydayChain.Hub.Domain.Sync;

/// <summary>
/// 定义 SyncTableDefinition 类型。
/// </summary>
public class SyncTableDefinition
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
    /// 获取或设置 SyncMode。
    /// </summary>
    public SyncMode SyncMode { get; set; } = SyncMode.KeyedMerge;

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
    public DateTime StartTimeLocal { get; set; }

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
    public SyncTablePriority Priority { get; set; } = SyncTablePriority.Low;

    /// <summary>
    /// 获取或设置 PageSize。
    /// </summary>
    public int PageSize { get; set; } = 5000;

    public IReadOnlyList<string> UniqueKeys { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> ExcludedColumns { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 获取或设置 DeletionPolicy。
    /// </summary>
    public DeletionPolicy DeletionPolicy { get; set; } = DeletionPolicy.Disabled;

    /// <summary>
    /// 获取或设置 DeletionEnabled。
    /// </summary>
    public bool DeletionEnabled { get; set; }

    /// <summary>
    /// 获取或设置 DeletionDryRun。
    /// </summary>
    public bool DeletionDryRun { get; set; }

    /// <summary>
    /// 获取或设置 DeletionCompareSegmentSize。
    /// </summary>
    public int DeletionCompareSegmentSize { get; set; } = 20000;

    /// <summary>
    /// 获取或设置 DeletionCompareMaxParallelism。
    /// </summary>
    public int DeletionCompareMaxParallelism { get; set; } = 4;

    /// <summary>
    /// 获取或设置 RetentionEnabled。
    /// </summary>
    public bool RetentionEnabled { get; set; }

    /// <summary>
    /// 获取或设置 RetentionKeepMonths。
    /// </summary>
    public int RetentionKeepMonths { get; set; } = 3;

    /// <summary>
    /// 获取或设置 RetentionDryRun。
    /// </summary>
    public bool RetentionDryRun { get; set; } = true;

    /// <summary>
    /// 获取或设置 RetentionAllowDrop。
    /// </summary>
    public bool RetentionAllowDrop { get; set; }

    /// <summary>
    /// 获取或设置 StatusConsumeProfile。
    /// </summary>
    public RemoteStatusConsumeProfile? StatusConsumeProfile { get; set; }

    /// <summary>
    /// 获取或设置 SourceType。
    /// </summary>
    public BusinessTaskSourceType SourceType { get; set; } = BusinessTaskSourceType.Unknown;

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

