using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 删除执行结果。
/// </summary>
public class SyncDeletionExecutionResult
{
    /// <summary>删除识别总数。</summary>
    public int DetectedCount { get; set; }

    /// <summary>实际执行删除数量。</summary>
    public int DeletedCount { get; set; }

    /// <summary>删除审计日志集合。</summary>
    public IReadOnlyList<SyncDeletionLog> DeletionLogs { get; set; } = Array.Empty<SyncDeletionLog>();

    /// <summary>删除变更日志集合。</summary>
    public IReadOnlyList<SyncChangeLog> ChangeLogs { get; set; } = Array.Empty<SyncChangeLog>();
}
