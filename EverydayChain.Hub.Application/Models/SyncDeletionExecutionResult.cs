using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 SyncDeletionExecutionResult 类型。
/// </summary>
public class SyncDeletionExecutionResult
{
    /// <summary>
    /// 获取或设置 DetectedCount。
    /// </summary>
    public int DetectedCount { get; set; }

    /// <summary>
    /// 获取或设置 DeletedCount。
    /// </summary>
    public int DeletedCount { get; set; }

    public IReadOnlyList<SyncDeletionLog> DeletionLogs { get; set; } = Array.Empty<SyncDeletionLog>();

    public IReadOnlyList<SyncChangeLog> ChangeLogs { get; set; } = Array.Empty<SyncChangeLog>();
}

