namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 业务回传补偿执行结果，描述一次失败任务补偿重试的汇总状态。
/// </summary>
public sealed class FeedbackCompensationResult
{
    /// <summary>
    /// 本次补偿目标任务总数（按任务模式为 0 或 1，按批次模式为本轮失败任务数量）。
    /// </summary>
    public int TargetCount { get; set; }

    /// <summary>
    /// 本次实际发起重试的任务数。
    /// </summary>
    public int RetriedCount { get; set; }

    /// <summary>
    /// 本次重试成功的任务数。
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 本次重试失败的任务数。
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// 本次跳过的任务数（如任务不存在或状态不在失败集合）。
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// 失败原因描述；无失败时为空。
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// 是否整体执行成功（失败数为 0）。
    /// </summary>
    public bool IsSuccess => FailedCount == 0;
}
