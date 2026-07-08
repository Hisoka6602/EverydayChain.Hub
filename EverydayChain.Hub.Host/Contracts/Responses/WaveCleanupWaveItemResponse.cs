namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示待清理波次的单条汇总记录。
/// </summary>
public sealed class WaveCleanupWaveItemResponse
{
    /// <summary>
    /// 表示波次号。
    /// </summary>
    public string WaveId { get; set; } = string.Empty;

    /// <summary>
    /// 表示备注信息。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 表示该波次对应的总包裹数量。
    /// </summary>
    public int PackageTotal { get; set; }

    /// <summary>
    /// 表示拆零任务总数量。
    /// </summary>
    public int SplitTotal { get; set; }

    /// <summary>
    /// 表示整件任务总数量。
    /// </summary>
    public int FullTotal { get; set; }

    /// <summary>
    /// 表示记录创建时间（本地时间）。
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 表示当前任务、波次或批次的业务状态。
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

