namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 增量分页读取结果。
/// </summary>
public class SyncReadResult
{
    /// <summary>当前页记录。</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; set; } = Array.Empty<IReadOnlyDictionary<string, object?>>();
}
