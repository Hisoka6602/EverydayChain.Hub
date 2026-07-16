namespace EverydayChain.Hub.SharedKernel.Utilities;

/// <summary>
/// 集中处理日志和审计文本的安全清洗。
/// </summary>
public static class LogTextUtility
{
    /// <summary>
    /// 将换行符转义为可见文本。
    /// </summary>
    /// <param name="value">原始文本。</param>
    /// <returns>转义后的文本。</returns>
    public static string EscapeLineBreaks(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }

    /// <summary>
    /// 移除换行符并保留其他文本。
    /// </summary>
    /// <param name="value">原始文本。</param>
    /// <returns>移除换行符后的文本。</returns>
    public static string RemoveLineBreaks(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
    }

    /// <summary>
    /// 移除控制字符并裁剪首尾空白。
    /// </summary>
    /// <param name="value">原始文本。</param>
    /// <returns>移除控制字符后的文本。</returns>
    public static string RemoveControlCharacters(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var chars = value
            .Where(ch => !char.IsControl(ch))
            .ToArray();
        return new string(chars).Trim();
    }

    /// <summary>
    /// 按最大长度裁剪文本。
    /// </summary>
    /// <param name="value">原始文本。</param>
    /// <param name="maxLength">允许的最大长度。</param>
    /// <returns>裁剪后的文本。</returns>
    public static string TrimToLength(string? value, int maxLength)
    {
        var normalizedValue = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        if (normalizedValue.Length <= maxLength)
        {
            return normalizedValue;
        }

        return normalizedValue[..maxLength];
    }

    /// <summary>
    /// 按最大长度截断文本并追加截断说明。
    /// </summary>
    /// <param name="value">原始文本。</param>
    /// <param name="maxLength">允许的最大长度。</param>
    /// <returns>带截断说明的文本。</returns>
    public static string TruncateWithSuffix(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value ?? string.Empty;
        }

        return $"{value[..maxLength]}...(已截断，原始长度={value.Length})";
    }
}
