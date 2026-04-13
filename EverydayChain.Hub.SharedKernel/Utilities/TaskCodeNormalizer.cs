namespace EverydayChain.Hub.SharedKernel.Utilities;

/// <summary>
/// 任务编码规范化工具。
/// </summary>
public static class TaskCodeNormalizer {
    /// <summary>
    /// 将任务编码规范化为去空白字符串；全空白时返回空字符串。
    /// </summary>
    /// <param name="taskCode">原始任务编码。</param>
    /// <returns>规范化后的任务编码。</returns>
    public static string NormalizeOrEmpty(string? taskCode) {
        return string.IsNullOrWhiteSpace(taskCode) ? string.Empty : taskCode.Trim();
    }
}
