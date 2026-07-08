namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 BusinessTaskFeedbackAggregate 类型。
/// </summary>
public sealed class BusinessTaskFeedbackAggregate
{
    /// <summary>
    /// 获取或设置 RequiredFeedbackCount。
    /// </summary>
    public int RequiredFeedbackCount { get; set; }

    /// <summary>
    /// 获取或设置 CompletedFeedbackCount。
    /// </summary>
    public int CompletedFeedbackCount { get; set; }
}

