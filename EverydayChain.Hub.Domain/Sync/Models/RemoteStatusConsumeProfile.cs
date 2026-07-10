namespace EverydayChain.Hub.Domain.Sync.Models;

/// <summary>
/// 定义 RemoteStatusConsumeProfile 类型。
/// </summary>
public class RemoteStatusConsumeProfile
{
    /// <summary>
    /// 获取或设置 StatusColumnName。
    /// </summary>
    public string StatusColumnName { get; set; } = "TASKPROCESS";

    /// <summary>
    /// 获取或设置 PendingStatusValue。
    /// </summary>
    public string? PendingStatusValue { get; set; } = "N";

    /// <summary>
    /// 获取或设置 IgnorePendingStatusValue。
    /// </summary>
    public bool IgnorePendingStatusValue { get; set; }

    /// <summary>
    /// 获取或设置 CompletedStatusValue。
    /// </summary>
    public string CompletedStatusValue { get; set; } = "Y";

    /// <summary>
    /// 获取或设置 ShouldWriteBackRemoteStatus。
    /// </summary>
    public bool ShouldWriteBackRemoteStatus { get; set; } = true;

    /// <summary>
    /// 获取或设置 BatchSize。
    /// </summary>
    public int BatchSize { get; set; } = 5000;

    /// <summary>
    /// 获取或设置 WriteBackCompletedTimeColumnName。
    /// </summary>
    public string? WriteBackCompletedTimeColumnName { get; set; }

    /// <summary>
    /// 获取或设置 WriteBackBatchIdColumnName。
    /// </summary>
    public string? WriteBackBatchIdColumnName { get; set; }
}

