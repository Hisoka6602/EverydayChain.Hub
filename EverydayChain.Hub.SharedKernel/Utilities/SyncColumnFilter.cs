namespace EverydayChain.Hub.SharedKernel.Utilities;

/// <summary>
/// 同步列过滤组件，提供排除列过滤与列名规范化能力。
/// </summary>
public static class SyncColumnFilter
{
    /// <summary>
    /// 软删除标记列名。
    /// </summary>
    public const string SoftDeleteFlagColumn = "IsDeleted";

    /// <summary>
    /// 软删除时间列名。
    /// </summary>
    public const string SoftDeleteTimeColumn = "DeletedTimeLocal";

    /// <summary>
    /// 软删除关键列集合。
    /// </summary>
    public static readonly IReadOnlySet<string> SoftDeleteColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        SoftDeleteFlagColumn,
        SoftDeleteTimeColumn,
    };

    /// <summary>
    /// 规范化后的软删除关键列集合。
    /// </summary>
    public static readonly IReadOnlySet<string> NormalizedSoftDeleteColumns = SoftDeleteColumns;

    /// <summary>
    /// 过滤行中的排除列（使用已规范化集合）。
    /// </summary>
    /// <param name="row">原始数据行。</param>
    /// <param name="normalizedExcludedColumns">规范化排除列集合。</param>
    /// <returns>过滤后的数据行。</returns>
    public static IReadOnlyDictionary<string, object?> FilterExcludedColumns(
        IReadOnlyDictionary<string, object?> row,
        IReadOnlySet<string> normalizedExcludedColumns)
    {
        if (normalizedExcludedColumns.Count == 0)
        {
            return row;
        }

        var filtered = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in row)
        {
            if (!normalizedExcludedColumns.Contains(pair.Key))
            {
                filtered[pair.Key] = pair.Value;
            }
        }

        return filtered;
    }

    /// <summary>
    /// 规范化列名集合并按忽略大小写去重。
    /// </summary>
    /// <param name="columns">原始列名集合。</param>
    /// <returns>规范化后的列名集合。</returns>
    public static HashSet<string> NormalizeColumns(IEnumerable<string> columns)
    {
        columns ??= Array.Empty<string>();

        return columns
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(static x => NormalizeColumnName(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 规范化单个列名。
    /// </summary>
    /// <param name="columnName">列名。</param>
    /// <returns>规范化后的列名，空白返回空字符串。</returns>
    public static string NormalizeColumnName(string columnName)
    {
        return string.IsNullOrWhiteSpace(columnName) ? string.Empty : columnName.Trim();
    }
}
