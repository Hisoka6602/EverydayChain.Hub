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
}
