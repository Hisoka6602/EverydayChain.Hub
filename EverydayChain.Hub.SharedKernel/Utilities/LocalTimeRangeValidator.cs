namespace EverydayChain.Hub.SharedKernel.Utilities;

/// <summary>
/// 定义 LocalTimeRangeValidator 类型。
/// </summary>
public static class LocalTimeRangeValidator
{
    /// <summary>
    /// 执行 TryNormalizeRequiredRange 方法。
    /// </summary>
    public static bool TryNormalizeRequiredRange(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        out DateTime normalizedStart,
        out DateTime normalizedEnd,
        out string validationMessage)
    {
        // 步骤：执行 TryNormalizeRequiredRange 方法的核心处理流程。
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
    /// 执行 TryNormalizeOptionalRange 方法。
    /// </summary>
    public static bool TryNormalizeOptionalRange(
        DateTime? startTimeLocal,
        DateTime? endTimeLocal,
        DateTime defaultStartLocal,
        out DateTime normalizedStart,
        out DateTime normalizedEnd,
        out string validationMessage)
    {
        // 步骤：执行 if 方法的核心处理流程。
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
