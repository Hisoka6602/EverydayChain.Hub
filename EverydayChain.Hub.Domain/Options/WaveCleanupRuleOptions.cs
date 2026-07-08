namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义 WaveCleanupRuleOptions 类型。
/// </summary>
public class WaveCleanupRuleOptions
{
    /// <summary>
    /// 获取或设置 Enabled。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 获取或设置 TargetStatusOnCleanup。
    /// </summary>
    public string TargetStatusOnCleanup { get; set; } = "Exception";
}

