using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 增量分页读取请求。
/// </summary>
public class SyncReadRequest
{
    /// <summary>表编码。</summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>游标列名。</summary>
    public string CursorColumn { get; set; } = string.Empty;

    /// <summary>页码（从 1 开始）。</summary>
    public int PageNo { get; set; }

    /// <summary>分页大小。</summary>
    public int PageSize { get; set; }

    /// <summary>同步窗口。</summary>
    public SyncWindow Window { get; set; }

    /// <summary>唯一键集合。</summary>
    public IReadOnlyList<string> UniqueKeys { get; set; } = Array.Empty<string>();

    /// <summary>规范化后的排除列集合。</summary>
    public IReadOnlySet<string> NormalizedExcludedColumns { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}
