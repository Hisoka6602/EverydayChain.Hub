using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncReadRequest
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string CursorColumn { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string SourceSchema { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string SourceTable { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int PageNo { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public SyncWindow Window { get; set; }

    public IReadOnlyList<string> UniqueKeys { get; set; } = Array.Empty<string>();

    public IReadOnlySet<string> NormalizedExcludedColumns { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}

