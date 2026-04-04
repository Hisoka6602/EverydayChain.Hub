namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 幂等合并请求。
/// 传入的行数据应已由源端完成列过滤，合并仓储不再重复过滤。
/// </summary>
public class SyncMergeRequest
{
    /// <summary>表编码。</summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>游标列名。</summary>
    public string CursorColumn { get; set; } = string.Empty;

    /// <summary>唯一键集合。</summary>
    public IReadOnlyList<string> UniqueKeys { get; set; } = Array.Empty<string>();

    /// <summary>待合并行（应已完成列过滤）。</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; set; } = Array.Empty<IReadOnlyDictionary<string, object?>>();
}
