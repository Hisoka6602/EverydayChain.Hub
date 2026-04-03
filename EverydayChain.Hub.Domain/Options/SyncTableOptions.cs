namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 单表同步配置。
/// </summary>
public class SyncTableOptions
{
    /// <summary>表编码。</summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>是否启用。</summary>
    public bool Enabled { get; set; }

    /// <summary>源端 Schema。</summary>
    public string SourceSchema { get; set; } = string.Empty;

    /// <summary>源端表名。</summary>
    public string SourceTable { get; set; } = string.Empty;

    /// <summary>目标逻辑表名。</summary>
    public string TargetLogicalTable { get; set; } = string.Empty;

    /// <summary>游标列名。</summary>
    public string CursorColumn { get; set; } = string.Empty;

    /// <summary>起始本地时间字符串。</summary>
    public string StartTimeLocal { get; set; } = string.Empty;

    /// <summary>分页大小。</summary>
    public int PageSize { get; set; } = 5000;

    /// <summary>轮询间隔（秒）。</summary>
    public int PollingIntervalSeconds { get; set; } = 60;

    /// <summary>最大滞后分钟数。</summary>
    public int MaxLagMinutes { get; set; } = 10;

    /// <summary>同步优先级（High/Low）。</summary>
    public string Priority { get; set; } = "Low";

    /// <summary>唯一键集合。</summary>
    public List<string> UniqueKeys { get; set; } = [];

    /// <summary>排除列集合。</summary>
    public List<string> ExcludedColumns { get; set; } = [];

    /// <summary>删除同步配置。</summary>
    public SyncDeleteOptions Delete { get; set; } = new();

    /// <summary>保留期治理配置。</summary>
    public SyncRetentionOptions Retention { get; set; } = new();
}
