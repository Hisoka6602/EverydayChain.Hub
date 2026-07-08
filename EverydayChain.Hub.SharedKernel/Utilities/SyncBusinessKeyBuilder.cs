using System.Globalization;

namespace EverydayChain.Hub.SharedKernel.Utilities;

/// <summary>
/// 定义 SyncBusinessKeyBuilder 类型。
/// </summary>
public static class SyncBusinessKeyBuilder
{
    /// <summary>
    /// 存储 LocalDateTimeBusinessKeyFormat 字段。
    /// </summary>
    private const string LocalDateTimeBusinessKeyFormat = "yyyy-MM-dd HH:mm:ss.fffffff";

    public static string Build(IReadOnlyDictionary<string, object?> row, IReadOnlyList<string> uniqueKeys)
    {
        if (uniqueKeys.Count == 0)
        {
            return string.Empty;
        }

        return string.Join("|", uniqueKeys.Select(key =>
            row.TryGetValue(key, out var value) ? ConvertToStableBusinessKeyComponent(value) : string.Empty));
    }

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

        throw new InvalidOperationException($"业务键中的时间值必须为本地时间。Kind={value.Kind}，Original={originalText ?? value.ToString("O", CultureInfo.InvariantCulture)}");
    }
}
