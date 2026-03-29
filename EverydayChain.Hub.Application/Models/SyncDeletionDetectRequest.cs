using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 删除差异识别请求。
/// </summary>
public class SyncDeletionDetectRequest
{
    /// <summary>表编码。</summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>游标列名。</summary>
    public string CursorColumn { get; set; } = string.Empty;

    /// <summary>源端 Schema。</summary>
    public string SourceSchema { get; set; } = string.Empty;

    /// <summary>源端表名。</summary>
    public string SourceTable { get; set; } = string.Empty;

    /// <summary>同步窗口。</summary>
    public SyncWindow Window { get; set; } = new();

    /// <summary>唯一键集合。</summary>
    public IReadOnlyList<string> UniqueKeys { get; set; } = Array.Empty<string>();

    /// <summary>删除比对分段大小。</summary>
    public int CompareSegmentSize { get; set; } = 20000;

    /// <summary>删除比对最大并行度。</summary>
    public int CompareMaxParallelism { get; set; } = 4;
}
