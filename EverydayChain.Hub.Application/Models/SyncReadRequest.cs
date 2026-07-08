using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 SyncReadRequest 类型。
/// </summary>
public class SyncReadRequest
{
    /// <summary>
    /// 获取或设置 TableCode。
    /// </summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 CursorColumn。
    /// </summary>
    public string CursorColumn { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 SourceSchema。
    /// </summary>
    public string SourceSchema { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 SourceTable。
    /// </summary>
    public string SourceTable { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 PageNo。
    /// </summary>
    public int PageNo { get; set; }

    /// <summary>
    /// 获取或设置 PageSize。
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 获取或设置 Window。
    /// </summary>
    public SyncWindow Window { get; set; }

    public IReadOnlyList<string> UniqueKeys { get; set; } = Array.Empty<string>();

    public IReadOnlySet<string> NormalizedExcludedColumns { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}

