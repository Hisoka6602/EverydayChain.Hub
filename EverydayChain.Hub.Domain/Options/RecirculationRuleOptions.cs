namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义 RecirculationRuleOptions 类型。
/// </summary>
public class RecirculationRuleOptions
{
    /// <summary>
    /// 获取或设置 Enabled。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 获取或设置 MaxScanRetries。
    /// </summary>
    public int MaxScanRetries { get; set; } = 3;
}

