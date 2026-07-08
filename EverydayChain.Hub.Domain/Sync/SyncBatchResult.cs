namespace EverydayChain.Hub.Domain.Sync;

/// <summary>
/// 表示一次同步批次执行完成后的统计结果。
/// </summary>
public class SyncBatchResult
{
    /// <summary>
    /// 获取或设置同步批次标识。
    /// </summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置同步表编码。
    /// </summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置本批次同步窗口起始时间。
    /// </summary>
    public DateTime WindowStartLocal { get; set; }

    /// <summary>
    /// 获取或设置本批次同步窗口结束时间。
    /// </summary>
    public DateTime WindowEndLocal { get; set; }

    /// <summary>
    /// 获取或设置本批次读取行数。
    /// </summary>
    public int ReadCount { get; set; }

    /// <summary>
    /// 获取或设置本批次新增行数。
    /// </summary>
    public int InsertCount { get; set; }

    /// <summary>
    /// 获取或设置本批次更新行数。
    /// </summary>
    public int UpdateCount { get; set; }

    /// <summary>
    /// 获取或设置本批次删除行数。
    /// </summary>
    public int DeleteCount { get; set; }

    /// <summary>
    /// 获取或设置本批次跳过行数。
    /// </summary>
    public int SkipCount { get; set; }

    /// <summary>
    /// 获取或设置本批次总耗时。
    /// </summary>
    public TimeSpan Elapsed { get; set; }

    /// <summary>
    /// 获取或设置同步窗口结束到当前时刻的滞后分钟数，保留三位小数。
    /// </summary>
    public decimal LagMinutes { get; set; }

    /// <summary>
    /// 获取或设置同步窗口开始到当前时刻的积压分钟数，保留三位小数。
    /// </summary>
    public decimal BacklogMinutes { get; set; }

    /// <summary>
    /// 获取或设置每秒处理行数，保留三位小数。
    /// </summary>
    public decimal ThroughputRowsPerSecond { get; set; }

    /// <summary>
    /// 获取或设置失败占比，取值范围为 0 到 1，保留三位小数。
    /// </summary>
    public decimal FailureRate { get; set; }

    /// <summary>
    /// 获取或设置失败原因说明。
    /// </summary>
    public string? FailureMessage { get; set; }
}

