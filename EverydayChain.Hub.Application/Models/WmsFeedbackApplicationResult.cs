namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 业务回传执行结果，描述一次批量回传操作的汇总状态。
/// </summary>
public sealed class WmsFeedbackApplicationResult
{
    /// <summary>
    /// 本次查询到的待回传任务总数。
    /// </summary>
    public int PendingCount { get; set; }

    /// <summary>
    /// 本次成功回传的任务数。
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 本次回传失败的任务数。
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// 是否整体执行成功（失败数为 0）。
    /// </summary>
    public bool IsSuccess => FailedCount == 0;

    /// <summary>
    /// 失败原因描述；无失败时为空。
    /// </summary>
    public string? FailureReason { get; set; }
}
