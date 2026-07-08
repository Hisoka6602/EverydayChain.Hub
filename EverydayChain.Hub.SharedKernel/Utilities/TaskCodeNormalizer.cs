namespace EverydayChain.Hub.SharedKernel.Utilities;

/// <summary>
/// 定义 TaskCodeNormalizer 类型。
/// </summary>
public static class TaskCodeNormalizer {
    /// <summary>
    /// 执行 NormalizeOrEmpty 方法。
    /// </summary>
    public static string NormalizeOrEmpty(string? taskCode) {
        // 步骤：执行 NormalizeOrEmpty 方法的核心处理流程。
        return string.IsNullOrWhiteSpace(taskCode) ? string.Empty : taskCode.Trim();
    }
}
