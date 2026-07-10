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
    /// 获取或设置 AggregateTimeBucketSeconds。
    /// </summary>
    public int AggregateTimeBucketSeconds { get; set; } = 120;

    /// <summary>
    /// 获取或设置 GlobalDashboardSeconds。
    /// </summary>
    public int GlobalDashboardSeconds { get; set; } = 60;

    /// <summary>
    /// 获取或设置 DockDashboardSeconds。
    /// </summary>
    public int DockDashboardSeconds { get; set; } = 60;

    /// <summary>
    /// 获取或设置 SortingReportSeconds。
    /// </summary>
    public int SortingReportSeconds { get; set; } = 60;

    /// <summary>
    /// 获取或设置 CurrentWaveSeconds。
    /// </summary>
    public int CurrentWaveSeconds { get; set; } = 30;

    /// <summary>
    /// 获取或设置 WaveOptionsSeconds。
    /// </summary>
    public int WaveOptionsSeconds { get; set; } = 60;

    /// <summary>
    /// 获取或设置 WaveListSeconds。
    /// </summary>
    public int WaveListSeconds { get; set; } = 60;

    /// <summary>
    /// 获取或设置 WaveSummarySeconds。
    /// </summary>
    public int WaveSummarySeconds { get; set; } = 60;

    /// <summary>
    /// 获取或设置 WaveZoneSeconds。
    /// </summary>
    public int WaveZoneSeconds { get; set; } = 60;

    /// <summary>
    /// 获取或设置 WaveDetailSeconds。
    /// </summary>
    public int WaveDetailSeconds { get; set; } = 30;

    /// <summary>
    /// 获取或设置 WaveCleanupSeconds。
    /// </summary>
    public int WaveCleanupSeconds { get; set; } = 30;

    /// <summary>
    /// 获取或设置 RecirculationSummarySeconds。
    /// </summary>
    public int RecirculationSummarySeconds { get; set; } = 60;

    /// <summary>
    /// 获取或设置 BusinessTaskQuerySeconds。
    /// </summary>
    public int BusinessTaskQuerySeconds { get; set; } = 2;

    /// <summary>
    /// 获取或设置 BusinessTaskExceptionSeconds。
    /// </summary>
    public int BusinessTaskExceptionSeconds { get; set; } = 2;

    /// <summary>
    /// 获取或设置 BusinessTaskRecirculationSeconds。
    /// </summary>
    public int BusinessTaskRecirculationSeconds { get; set; } = 2;

    /// <summary>
    /// 获取或设置 BoxTrackingSeconds。
    /// </summary>
    public int BoxTrackingSeconds { get; set; } = 30;

    /// <summary>
    /// 获取或设置 ChuteResolveSeconds。
    /// </summary>
    public int ChuteResolveSeconds { get; set; } = 2;

    /// <summary>
    /// 获取或设置 RetentionCleanupSeconds。
    /// </summary>
    public int RetentionCleanupSeconds { get; set; } = 30;

    /// <summary>
    /// 获取或设置 BackgroundWarmupEnabled。
    /// </summary>
    public bool BackgroundWarmupEnabled { get; set; } = true;

    /// <summary>
    /// 获取或设置 BackgroundWarmupIntervalSeconds。
    /// </summary>
    public int BackgroundWarmupIntervalSeconds { get; set; } = 15;
}

