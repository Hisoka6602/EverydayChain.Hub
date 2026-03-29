namespace EverydayChain.Hub.Domain.Sync;

/// <summary>
/// 同步业务键构建器。
/// </summary>
public static class SyncBusinessKeyBuilder
{
    /// <summary>
    /// 根据唯一键配置构建业务键文本。
    /// </summary>
    /// <param name="row">数据行。</param>
    /// <param name="uniqueKeys">唯一键集合。</param>
    /// <returns>业务键文本。</returns>
    public static string Build(IReadOnlyDictionary<string, object?> row, IReadOnlyList<string> uniqueKeys)
    {
        if (uniqueKeys.Count == 0)
        {
            return string.Empty;
        }

        return string.Join("|", uniqueKeys.Select(key =>
            row.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty));
    }
}
