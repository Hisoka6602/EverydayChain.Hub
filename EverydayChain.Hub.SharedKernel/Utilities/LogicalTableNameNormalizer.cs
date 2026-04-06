using System.Text.RegularExpressions;

namespace EverydayChain.Hub.SharedKernel.Utilities;

/// <summary>
/// 逻辑表名规范化与安全校验工具。
/// </summary>
public static class LogicalTableNameNormalizer
{
    /// <summary>安全标识符校验正则（仅允许字母、数字、下划线）。</summary>
    private static readonly Regex SqlIdentifierRegex = new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    /// <summary>
    /// 规范化逻辑表名。
    /// </summary>
    /// <param name="logicalTable">待规范化逻辑表名。</param>
    /// <returns>去除首尾空白后的逻辑表名；空白输入返回 <c>null</c>。</returns>
    public static string? NormalizeOrNull(string? logicalTable)
    {
        if (string.IsNullOrWhiteSpace(logicalTable))
        {
            return null;
        }

        return logicalTable.Trim();
    }

    /// <summary>
    /// 判断逻辑表名是否满足 SQL 标识符安全规则。
    /// </summary>
    /// <param name="identifier">待校验逻辑表名。</param>
    /// <returns>满足规则返回 <c>true</c>。</returns>
    public static bool IsSafeSqlIdentifier(string identifier)
    {
        return !string.IsNullOrWhiteSpace(identifier) && SqlIdentifierRegex.IsMatch(identifier);
    }

    /// <summary>
    /// 校验并写入逻辑表名集合。
    /// </summary>
    /// <param name="target">目标集合。</param>
    /// <param name="logicalTable">待写入逻辑表名。</param>
    /// <param name="sourcePath">配置来源路径。</param>
    /// <exception cref="InvalidOperationException">表名不满足安全规则时抛出。</exception>
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
            throw new InvalidOperationException($"分表配置无效：{sourcePath} 包含非法逻辑表名 '{normalized}'，仅允许字母、数字、下划线。");
        }

        target.Add(normalized);
    }
}
