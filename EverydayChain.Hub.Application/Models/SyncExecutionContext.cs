using EverydayChain.Hub.SharedKernel.Utilities;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 同步执行上下文。
/// </summary>
public class SyncExecutionContext
{
    /// <summary>表定义。</summary>
    public SyncTableDefinition Definition { get; set; } = new();

    /// <summary>检查点。</summary>
    public SyncCheckpoint Checkpoint { get; set; } = new();

    /// <summary>同步窗口。</summary>
    public SyncWindow Window { get; set; }

    /// <summary>批次编号。</summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>父批次编号（重试关联）。</summary>
    public string? ParentBatchId { get; set; }

    /// <summary>规范化后的排除列集合。</summary>
    public IReadOnlySet<string> NormalizedExcludedColumns { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}
