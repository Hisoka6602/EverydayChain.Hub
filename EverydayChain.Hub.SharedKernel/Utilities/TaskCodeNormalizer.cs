namespace EverydayChain.Hub.SharedKernel.Utilities;

/// <summary>
/// 定义当前类型。
/// </summary>
public static class TaskCodeNormalizer {
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public static string NormalizeOrEmpty(string? taskCode) {
        // 步骤：按既定流程执行当前方法逻辑。
        return string.IsNullOrWhiteSpace(taskCode) ? string.Empty : taskCode.Trim();
    }
}
