namespace EverydayChain.Hub.SharedKernel.Utilities;

/// <summary>
/// 定义当前类型。
/// </summary>
public static class LocalDateTimeNormalizer {
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public static bool TryNormalize(DateTime candidateTime, string invalidKindMessage, out DateTime normalizedTime, out string validationMessage) {
        // 步骤：按既定流程执行当前方法逻辑。
        if (candidateTime.Kind != DateTimeKind.Local && candidateTime.Kind != DateTimeKind.Unspecified) {
            normalizedTime = default;
            validationMessage = invalidKindMessage;
            return false;
        }

        if (candidateTime == DateTime.MinValue) {
            normalizedTime = DateTime.Now;
            validationMessage = string.Empty;
            return true;
        }

        if (candidateTime.Kind == DateTimeKind.Unspecified) {
            normalizedTime = DateTime.SpecifyKind(candidateTime, DateTimeKind.Local);
            validationMessage = string.Empty;
            return true;
        }

        normalizedTime = candidateTime;
        validationMessage = string.Empty;
        return true;
    }
}
