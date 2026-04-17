namespace EverydayChain.Hub.SharedKernel.Utilities;

/// <summary>
/// 本地时间区间校验工具。
/// </summary>
public static class LocalTimeRangeValidator
{
    /// <summary>
    /// 校验必填时间区间并规范化为本地时间语义。
    /// </summary>
    /// <param name="startTimeLocal">开始时间（本地时间，包含）。</param>
    /// <param name="endTimeLocal">结束时间（本地时间，不包含）。</param>
    /// <param name="normalizedStart">规范化后的开始时间。</param>
    /// <param name="normalizedEnd">规范化后的结束时间。</param>
    /// <param name="validationMessage">校验失败消息。</param>
    /// <returns>是否通过校验。</returns>
    public static bool TryNormalizeRequiredRange(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        out DateTime normalizedStart,
        out DateTime normalizedEnd,
        out string validationMessage)
    {
        normalizedStart = default;
        normalizedEnd = default;
        validationMessage = string.Empty;

        if (startTimeLocal == DateTime.MinValue)
        {
            validationMessage = "开始时间不能为空。";
            return false;
        }

        if (endTimeLocal == DateTime.MinValue)
        {
            validationMessage = "结束时间不能为空。";
            return false;
        }

        if (!LocalDateTimeNormalizer.TryNormalize(startTimeLocal, "开始时间必须为本地时间，禁止传入 UTC 时间。", out normalizedStart, out var startValidationMessage))
        {
            validationMessage = startValidationMessage;
            return false;
        }

        if (!LocalDateTimeNormalizer.TryNormalize(endTimeLocal, "结束时间必须为本地时间，禁止传入 UTC 时间。", out normalizedEnd, out var endValidationMessage))
        {
            validationMessage = endValidationMessage;
            return false;
        }

        if (normalizedEnd <= normalizedStart)
        {
            validationMessage = "结束时间必须大于开始时间。";
            return false;
        }

        return true;
    }

    /// <summary>
    /// 校验可选时间区间并规范化为本地时间语义。
    /// </summary>
    /// <param name="startTimeLocal">可选开始时间（本地时间，包含）。</param>
    /// <param name="endTimeLocal">可选结束时间（本地时间，不包含）。</param>
    /// <param name="defaultStartLocal">默认开始时间（本地时间）。</param>
    /// <param name="normalizedStart">规范化后的开始时间。</param>
    /// <param name="normalizedEnd">规范化后的结束时间。</param>
    /// <param name="validationMessage">校验失败消息。</param>
    /// <returns>是否通过校验。</returns>
    public static bool TryNormalizeOptionalRange(
        DateTime? startTimeLocal,
        DateTime? endTimeLocal,
        DateTime defaultStartLocal,
        out DateTime normalizedStart,
        out DateTime normalizedEnd,
        out string validationMessage)
    {
        normalizedStart = default;
        normalizedEnd = default;
        validationMessage = string.Empty;

        var startCandidate = startTimeLocal;
        var endCandidate = endTimeLocal;
        if (startCandidate is null && endCandidate is null)
        {
            startCandidate = defaultStartLocal;
            endCandidate = defaultStartLocal.AddDays(1);
        }
        else if (startCandidate is null)
        {
            startCandidate = defaultStartLocal;
        }

        if (!LocalDateTimeNormalizer.TryNormalize(startCandidate.Value, "开始时间必须为本地时间，禁止传入 UTC 时间。", out normalizedStart, out var startValidationMessage))
        {
            validationMessage = startValidationMessage;
            return false;
        }

        var resolvedEndCandidate = endCandidate ?? normalizedStart.AddDays(1);
        if (!LocalDateTimeNormalizer.TryNormalize(resolvedEndCandidate, "结束时间必须为本地时间，禁止传入 UTC 时间。", out normalizedEnd, out var endValidationMessage))
        {
            validationMessage = endValidationMessage;
            return false;
        }

        if (normalizedEnd <= normalizedStart)
        {
            validationMessage = "结束时间必须大于开始时间。";
            return false;
        }

        return true;
    }
}
