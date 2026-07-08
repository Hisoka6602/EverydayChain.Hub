namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 SyncMergeRequest 类型。
/// </summary>
public class SyncMergeRequest
{
    /// <summary>
    /// 获取或设置 TableCode。
    /// </summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 CursorColumn。
    /// </summary>
    public string CursorColumn { get; set; } = string.Empty;

    public IReadOnlyList<string> UniqueKeys { get; set; } = Array.Empty<string>();

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; set; } = Array.Empty<IReadOnlyDictionary<string, object?>>();

    public IReadOnlySet<string> NormalizedExcludedColumns { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}

