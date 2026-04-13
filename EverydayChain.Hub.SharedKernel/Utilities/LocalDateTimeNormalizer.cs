namespace EverydayChain.Hub.SharedKernel.Utilities;

/// <summary>
/// 本地时间规范化工具。
/// </summary>
public static class LocalDateTimeNormalizer {
    /// <summary>
    /// 尝试将输入时间规范化为本地时间语义。
    /// </summary>
    /// <param name="candidateTime">候选时间。</param>
    /// <param name="invalidKindMessage">当输入不是本地或未指定语义时返回的错误消息。</param>
    /// <param name="normalizedTime">规范化后的时间。</param>
    /// <param name="validationMessage">校验失败消息。</param>
    /// <returns>校验是否通过。</returns>
    public static bool TryNormalize(DateTime candidateTime, string invalidKindMessage, out DateTime normalizedTime, out string validationMessage) {
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
