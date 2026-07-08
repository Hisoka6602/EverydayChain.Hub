using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 SyncExecutionContext 类型。
/// </summary>
public class SyncExecutionContext
{
    public SyncTableDefinition Definition { get; set; } = new();

    public SyncCheckpoint Checkpoint { get; set; } = new();

    /// <summary>
    /// 获取或设置 Window。
    /// </summary>
    public SyncWindow Window { get; set; }

    /// <summary>
    /// 获取或设置 BatchId。
    /// </summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 ParentBatchId。
    /// </summary>
    public string? ParentBatchId { get; set; }

    public IReadOnlySet<string> NormalizedExcludedColumns { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}

