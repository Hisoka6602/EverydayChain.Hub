using System.Text.RegularExpressions;

namespace EverydayChain.Hub.SharedKernel.Utilities;

/// <summary>
/// 定义 LogicalTableNameNormalizer 类型。
/// </summary>
public static class LogicalTableNameNormalizer
{
    /// <summary>
    /// 校验逻辑表名只能包含 ASCII 字母、数字与下划线。
    /// </summary>
    private static readonly Regex SqlIdentifierRegex = new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    public static string? NormalizeOrNull(string? logicalTable)
    {
        if (string.IsNullOrWhiteSpace(logicalTable))
        {
            return null;
        }

        return logicalTable.Trim();
    }

    public static bool IsSafeSqlIdentifier(string identifier)
    {
        return !string.IsNullOrWhiteSpace(identifier) && SqlIdentifierRegex.IsMatch(identifier);
    }

    public static void AddValidated(HashSet<string> target, string? logicalTable, string sourcePath)
    {
        ArgumentNullException.ThrowIfNull(target);

        var normalized = NormalizeOrNull(logicalTable);
        if (normalized is null)
        {
            return;
        }

        if (!IsSafeSqlIdentifier(normalized))
        {
            throw new InvalidOperationException($"分表配置无效：{sourcePath} 包含非法逻辑表名 '{normalized}'，仅允许字母、数字和下划线。");
        }

        target.Add(normalized);
    }
}
