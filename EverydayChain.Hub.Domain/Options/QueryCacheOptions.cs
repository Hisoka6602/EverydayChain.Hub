namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义 QueryCacheOptions 类型。
/// </summary>
public sealed class QueryCacheOptions
{
    /// <summary>
    /// 存储 SectionName 字段。
    /// </summary>
    public const string SectionName = "QueryCache";

    /// <summary>
    /// 获取或设置 Enabled。
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 获取或设置 GlobalDashboardSeconds。
    /// </summary>
    public int GlobalDashboardSeconds { get; set; } = 2;

    /// <summary>
    /// 获取或设置 DockDashboardSeconds。
    /// </summary>
    public int DockDashboardSeconds { get; set; } = 2;

    /// <summary>
    /// 获取或设置 SortingReportSeconds。
    /// </summary>
    public int SortingReportSeconds { get; set; } = 10;

    /// <summary>
    /// 获取或设置 CurrentWaveSeconds。
    /// </summary>
    public int CurrentWaveSeconds { get; set; } = 1;

    /// <summary>
    /// 获取或设置 WaveOptionsSeconds。
    /// </summary>
    public int WaveOptionsSeconds { get; set; } = 5;

    /// <summary>
    /// 获取或设置 WaveListSeconds。
    /// </summary>
    public int WaveListSeconds { get; set; } = 5;

    /// <summary>
    /// 获取或设置 WaveSummarySeconds。
    /// </summary>
    public int WaveSummarySeconds { get; set; } = 5;

    /// <summary>
    /// 获取或设置 WaveZoneSeconds。
    /// </summary>
    public int WaveZoneSeconds { get; set; } = 5;

    /// <summary>
    /// 获取或设置 WaveCleanupSeconds。
    /// </summary>
    public int WaveCleanupSeconds { get; set; } = 5;

    /// <summary>
    /// 获取或设置 RecirculationSummarySeconds。
    /// </summary>
    public int RecirculationSummarySeconds { get; set; } = 5;
}

