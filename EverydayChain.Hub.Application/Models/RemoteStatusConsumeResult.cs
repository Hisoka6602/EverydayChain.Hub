namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 状态驱动消费结果，汇总一轮 StatusDriven 批次的读取、追加、回写统计。
/// </summary>
public class RemoteStatusConsumeResult
{
    /// <summary>本轮从 Oracle 读取的总行数。</summary>
    public int ReadCount { get; set; }

    /// <summary>成功追加到 SQL Server 目标表的总行数。</summary>
    public int AppendCount { get; set; }

    /// <summary>成功回写到 Oracle 远端状态的行数（按 ROWID 更新）。</summary>
    public int WriteBackCount { get; set; }

    /// <summary>回写 Oracle 远端状态失败的行数。</summary>
    public int WriteBackFailCount { get; set; }

    /// <summary>因缺少 __RowId 而跳过回写的行数（该行仍已成功追加到本地）。</summary>
    public int SkippedWriteBackCount { get; set; }

    /// <summary>实际处理的总页数（读取到空页为止）。</summary>
    public int PageCount { get; set; }
}
