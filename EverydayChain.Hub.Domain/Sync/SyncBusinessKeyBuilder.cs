using System.Text.Json;

namespace EverydayChain.Hub.Domain.Sync;

/// <summary>
/// 同步业务键构建器。
/// </summary>
public static class SyncBusinessKeyBuilder
{
    /// <summary>业务键序列化配置。</summary>
    private static readonly JsonSerializerOptions BusinessKeySerializerOptions = new()
    {
        WriteIndented = false,
    };

    /// <summary>
    /// 根据唯一键配置构建业务键文本。
    /// </summary>
    /// <param name="uniqueKeys">唯一键集合。</param>
    /// <param name="row">数据行。</param>
    /// <returns>业务键文本。</returns>
    public static string Build(IReadOnlyList<string> uniqueKeys, IReadOnlyDictionary<string, object?> row)
    {
        if (uniqueKeys.Count == 0)
        {
            return string.Empty;
        }

        var keyValues = uniqueKeys.Select(key =>
            row.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty).ToArray();
        return JsonSerializer.Serialize(keyValues, BusinessKeySerializerOptions);
    }
}
