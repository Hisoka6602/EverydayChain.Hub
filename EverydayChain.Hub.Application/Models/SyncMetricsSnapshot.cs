namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 同步指标快照。
/// </summary>
public class SyncMetricsSnapshot
{
    /// <summary>表编码。</summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>批次编号。</summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>窗口滞后分钟数。</summary>
    public double LagMinutes { get; set; }

    /// <summary>窗口积压分钟数。</summary>
    public double BacklogMinutes { get; set; }

    /// <summary>吞吐（每秒处理行数）。</summary>
    public double ThroughputRowsPerSecond { get; set; }

    /// <summary>失败率（0~1）。</summary>
    public double FailureRate { get; set; }
}
