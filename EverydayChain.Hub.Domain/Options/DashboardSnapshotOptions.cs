namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class DashboardSnapshotOptions
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    public const string SectionName = "DashboardSnapshot";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool PreferSnapshotQueries { get; set; } = true;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool AllowBaseTableFallback { get; set; } = true;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int RefreshIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int InitialBackfillHours { get; set; } = 168;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int RefreshOverlapSeconds { get; set; } = 30;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int RefreshLeaseSeconds { get; set; } = 120;
}

