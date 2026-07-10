namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 RemoteStatusConsumeResult 类型。
/// </summary>
public class RemoteStatusConsumeResult
{
    /// <summary>
    /// 获取或设置 ReadCount。
    /// </summary>
    public int ReadCount { get; set; }

    /// <summary>
    /// 获取或设置 AppendCount。
    /// </summary>
    public int AppendCount { get; set; }

    /// <summary>
    /// 获取或设置 WriteBackCount。
    /// </summary>
    public int WriteBackCount { get; set; }

    /// <summary>
    /// 获取或设置 WriteBackFailCount。
    /// </summary>
    public int WriteBackFailCount { get; set; }

    /// <summary>
    /// 获取或设置 SkippedWriteBackCount。
    /// </summary>
    public int SkippedWriteBackCount { get; set; }

    /// <summary>
    /// 获取或设置 PageCount。
    /// </summary>
    public int PageCount { get; set; }

    /// <summary>
    /// 获取或设置 LastSuccessCursorLocal。
    /// </summary>
    public DateTime? LastSuccessCursorLocal { get; set; }
}

