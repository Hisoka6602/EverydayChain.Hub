namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class QueryCacheOptions
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    public const string SectionName = "QueryCache";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int GlobalDashboardSeconds { get; set; } = 2;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int DockDashboardSeconds { get; set; } = 2;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int SortingReportSeconds { get; set; } = 10;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int CurrentWaveSeconds { get; set; } = 1;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int WaveOptionsSeconds { get; set; } = 5;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int WaveListSeconds { get; set; } = 5;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int WaveSummarySeconds { get; set; } = 5;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int WaveZoneSeconds { get; set; } = 5;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int WaveCleanupSeconds { get; set; } = 5;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int RecirculationSummarySeconds { get; set; } = 5;
}

