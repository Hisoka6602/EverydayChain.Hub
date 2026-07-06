namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class WmsFeedbackApplicationResult
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int PendingCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool IsSuccess => FailedCount == 0;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? FailureReason { get; set; }
}

