namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义当前类型。
/// </summary>
public class RecirculationRuleOptions
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int MaxScanRetries { get; set; } = 3;
}

