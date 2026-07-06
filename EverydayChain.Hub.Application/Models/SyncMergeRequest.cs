namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncMergeRequest
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string CursorColumn { get; set; } = string.Empty;

    public IReadOnlyList<string> UniqueKeys { get; set; } = Array.Empty<string>();

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; set; } = Array.Empty<IReadOnlyDictionary<string, object?>>();

    public IReadOnlySet<string> NormalizedExcludedColumns { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}

