using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.Sync;

/// <summary>
/// 单表同步定义，描述同步任务的核心配置。
/// </summary>
public class SyncTableDefinition
{
    /// <summary>表编码。</summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>是否启用。</summary>
    public bool Enabled { get; set; }

    /// <summary>同步模式。</summary>
    public SyncMode SyncMode { get; set; } = SyncMode.Incremental;

    /// <summary>源端 Schema。</summary>
    public string SourceSchema { get; set; } = string.Empty;

    /// <summary>源端表名。</summary>
    public string SourceTable { get; set; } = string.Empty;

    /// <summary>目标逻辑表名。</summary>
    public string TargetLogicalTable { get; set; } = string.Empty;

    /// <summary>游标列名。</summary>
    public string CursorColumn { get; set; } = string.Empty;

    /// <summary>起始本地时间。</summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>轮询间隔（秒）。</summary>
    public int PollingIntervalSeconds { get; set; } = 60;

    /// <summary>最大滞后分钟数。</summary>
    public int MaxLagMinutes { get; set; } = 10;

    /// <summary>分页大小。</summary>
    public int PageSize { get; set; } = 5000;

    /// <summary>唯一键集合。</summary>
    public IReadOnlyList<string> UniqueKeys { get; set; } = Array.Empty<string>();

    /// <summary>排除列集合。</summary>
    public IReadOnlyList<string> ExcludedColumns { get; set; } = Array.Empty<string>();
}
