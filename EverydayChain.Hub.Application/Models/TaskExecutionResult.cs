namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 TaskExecutionResult 类型。
/// </summary>
public sealed class TaskExecutionResult
{
    /// <summary>
    /// 获取或设置 IsSuccess。
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 获取或设置 TaskId。
    /// </summary>
    public long TaskId { get; set; }

    /// <summary>
    /// 获取或设置 TaskCode。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 TaskStatus。
    /// </summary>
    public string TaskStatus { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 FailureReason。
    /// </summary>
    public string FailureReason { get; set; } = string.Empty;
}

