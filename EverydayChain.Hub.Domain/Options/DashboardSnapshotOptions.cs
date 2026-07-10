namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义 DashboardSnapshotOptions 类型。
/// </summary>
public sealed class DashboardSnapshotOptions
{
    /// <summary>
    /// 存储 SectionName 字段。
    /// </summary>
    public const string SectionName = "DashboardSnapshot";

    /// <summary>
    /// 获取或设置 Enabled。
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 获取或设置 PreferSnapshotQueries。
    /// </summary>
    public bool PreferSnapshotQueries { get; set; } = true;

    /// <summary>
    /// 获取或设置 AllowBaseTableFallback。
    /// </summary>
    public bool AllowBaseTableFallback { get; set; } = true;

    /// <summary>
    /// 获取或设置 RefreshIntervalSeconds。
    /// </summary>
    public int RefreshIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// 获取或设置 SingleRunTimeoutSeconds。
    /// </summary>
    public int SingleRunTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// 获取或设置 InitialBackfillHours。
    /// </summary>
    public int InitialBackfillHours { get; set; } = 168;

    /// <summary>
    /// 获取或设置 RefreshOverlapSeconds。
    /// </summary>
    public int RefreshOverlapSeconds { get; set; } = 30;

    /// <summary>
    /// 获取或设置 RefreshLeaseSeconds。
    /// </summary>
    public int RefreshLeaseSeconds { get; set; } = 120;

    /// <summary>
    /// 获取或设置 ForceInitialFullRefresh。
    /// </summary>
    public bool ForceInitialFullRefresh { get; set; }
}

