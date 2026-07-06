namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public class RemoteStatusConsumeResult
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int ReadCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int AppendCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int WriteBackCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int WriteBackFailCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int SkippedWriteBackCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int PageCount { get; set; }
}

