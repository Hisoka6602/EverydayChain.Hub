using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Sync.Models;

namespace EverydayChain.Hub.Domain.Sync;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncTableDefinition
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
    public SyncMode SyncMode { get; set; } = SyncMode.KeyedMerge;

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
    public DateTime StartTimeLocal { get; set; }

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
    public SyncTablePriority Priority { get; set; } = SyncTablePriority.Low;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int PageSize { get; set; } = 5000;

    public IReadOnlyList<string> UniqueKeys { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> ExcludedColumns { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DeletionPolicy DeletionPolicy { get; set; } = DeletionPolicy.Disabled;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool DeletionEnabled { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool DeletionDryRun { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int DeletionCompareSegmentSize { get; set; } = 20000;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int DeletionCompareMaxParallelism { get; set; } = 4;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool RetentionEnabled { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int RetentionKeepMonths { get; set; } = 3;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool RetentionDryRun { get; set; } = true;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool RetentionAllowDrop { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public RemoteStatusConsumeProfile? StatusConsumeProfile { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public BusinessTaskSourceType SourceType { get; set; } = BusinessTaskSourceType.Unknown;

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

