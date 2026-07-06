using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncExecutionContext
{
    public SyncTableDefinition Definition { get; set; } = new();

    public SyncCheckpoint Checkpoint { get; set; } = new();

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public SyncWindow Window { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? ParentBatchId { get; set; }

    public IReadOnlySet<string> NormalizedExcludedColumns { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}

