namespace EverydayChain.Hub.Domain.Sync;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncBatchResult
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime WindowStartLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime WindowEndLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int ReadCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int InsertCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int UpdateCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int DeleteCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int SkipCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public TimeSpan Elapsed { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public double LagMinutes { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public double BacklogMinutes { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public double ThroughputRowsPerSecond { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public double FailureRate { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? FailureMessage { get; set; }
}

