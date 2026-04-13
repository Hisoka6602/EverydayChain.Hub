namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 任务执行结果模型，描述扫描执行后的状态变化。
/// </summary>
public sealed class TaskExecutionResult
{
    /// <summary>
    /// 执行是否成功。
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 任务主键标识。
    /// </summary>
    public long TaskId { get; set; }

    /// <summary>
    /// 任务编码。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 执行后的任务状态字符串。
    /// </summary>
    public string TaskStatus { get; set; } = string.Empty;

    /// <summary>
    /// 失败原因；成功时为空。
    /// </summary>
    public string FailureReason { get; set; } = string.Empty;
}
