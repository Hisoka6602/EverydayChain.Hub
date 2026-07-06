namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncReadResult
{
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; set; } = Array.Empty<IReadOnlyDictionary<string, object?>>();
}

