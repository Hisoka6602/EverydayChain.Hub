namespace EverydayChain.Hub.Domain.Sync.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public class RemoteStatusConsumeProfile
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string StatusColumnName { get; set; } = "TASKPROCESS";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? PendingStatusValue { get; set; } = "N";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string CompletedStatusValue { get; set; } = "Y";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool ShouldWriteBackRemoteStatus { get; set; } = true;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int BatchSize { get; set; } = 5000;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? WriteBackCompletedTimeColumnName { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? WriteBackBatchIdColumnName { get; set; }
}

