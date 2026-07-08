namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义 MultiLabelRuleOptions 类型。
/// </summary>
public class MultiLabelRuleOptions
{
    /// <summary>
    /// 获取或设置 Enabled。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 获取或设置 Strategy。
    /// </summary>
    public string Strategy { get; set; } = "MarkException";
}

