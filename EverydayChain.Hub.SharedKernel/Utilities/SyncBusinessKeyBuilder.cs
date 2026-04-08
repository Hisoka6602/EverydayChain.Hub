using System.Globalization;

namespace EverydayChain.Hub.SharedKernel.Utilities;

/// <summary>
/// 同步业务键构建器。
/// </summary>
public static class SyncBusinessKeyBuilder
{
    /// <summary>本地时间业务键稳定格式。</summary>
    private const string LocalDateTimeBusinessKeyFormat = "yyyy-MM-dd HH:mm:ss.fffffff";

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
            row.TryGetValue(key, out var value) ? ConvertToStableBusinessKeyComponent(value) : string.Empty));
    }

    /// <summary>
    /// 尝试将业务键组件文本解析为本地时间。
    /// </summary>
    /// <param name="value">组件文本。</param>
    /// <param name="localDateTime">解析得到的本地时间。</param>
    /// <returns>可解析返回 <c>true</c>。</returns>
    public static bool TryParseLocalDateTimeComponent(string value, out DateTime localDateTime)
    {
        if (!DateTime.TryParseExact(
                value,
                LocalDateTimeBusinessKeyFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces,
                out var parsedDateTime))
        {
            localDateTime = default;
            return false;
        }

        localDateTime = EnsureLocalDateTime(parsedDateTime, value);
        return true;
    }

    /// <summary>
    /// 转换业务键组件为稳定文本。
    /// </summary>
    /// <param name="value">原始值。</param>
    /// <returns>稳定文本。</returns>
    private static string ConvertToStableBusinessKeyComponent(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value is DateTime dateTime)
        {
            var localDateTime = EnsureLocalDateTime(dateTime, null);
            return localDateTime.ToString(LocalDateTimeBusinessKeyFormat, CultureInfo.InvariantCulture);
        }

        if (value is DateTimeOffset dateTimeOffset)
        {
            return EnsureLocalDateTime(dateTimeOffset.LocalDateTime, dateTimeOffset.ToString("O", CultureInfo.InvariantCulture))
                .ToString(LocalDateTimeBusinessKeyFormat, CultureInfo.InvariantCulture);
        }

        if (value is IFormattable formattable)
        {
            return formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        return value.ToString() ?? string.Empty;
    }

    /// <summary>
    /// 确保时间值满足本地时间语义。
    /// </summary>
    /// <param name="value">时间值。</param>
    /// <param name="originalText">原始文本。</param>
    /// <returns>本地语义时间值。</returns>
    private static DateTime EnsureLocalDateTime(DateTime value, string? originalText)
    {
        if (value.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Local);
        }

        if (value.Kind == DateTimeKind.Local)
        {
            return value;
        }

        throw new InvalidOperationException($"检测到非本地时间语义（Kind={value.Kind}），已拒绝加载：{originalText ?? value.ToString("O", CultureInfo.InvariantCulture)}");
    }
}
