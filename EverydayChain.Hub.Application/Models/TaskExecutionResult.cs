namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class TaskExecutionResult
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public long TaskId { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string TaskStatus { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string FailureReason { get; set; } = string.Empty;
}

