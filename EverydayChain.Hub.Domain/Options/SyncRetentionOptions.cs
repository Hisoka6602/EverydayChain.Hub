namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncRetentionOptions
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int KeepMonths { get; set; } = 3;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool DryRun { get; set; } = true;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool AllowDrop { get; set; }
}

