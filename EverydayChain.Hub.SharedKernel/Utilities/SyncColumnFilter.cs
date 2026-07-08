namespace EverydayChain.Hub.SharedKernel.Utilities;

/// <summary>
/// 定义 SyncColumnFilter 类型。
/// </summary>
public static class SyncColumnFilter
{
    /// <summary>
    /// 存储 SoftDeleteFlagColumn 字段。
    /// </summary>
    public const string SoftDeleteFlagColumn = "IsDeleted";

    /// <summary>
    /// 存储 SoftDeleteTimeColumn 字段。
    /// </summary>
    public const string SoftDeleteTimeColumn = "DeletedTimeLocal";

    public static readonly IReadOnlySet<string> SoftDeleteColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        SoftDeleteFlagColumn,
        SoftDeleteTimeColumn,
    };

    /// <summary>
    /// 执行 FilterExcludedColumns 方法。
    /// </summary>
    public static IReadOnlyDictionary<string, object?> FilterExcludedColumns(
        IReadOnlyDictionary<string, object?> row,
        IReadOnlySet<string> normalizedExcludedColumns)
    {
        // 步骤：执行 FilterExcludedColumns 方法的核心处理流程。
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

    public static HashSet<string> NormalizeColumns(IEnumerable<string> columns)
    {
        columns ??= Array.Empty<string>();

        return columns
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(static x => NormalizeColumnName(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public static string NormalizeColumnName(string columnName)
    {
        return string.IsNullOrWhiteSpace(columnName) ? string.Empty : columnName.Trim();
    }
}
