namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义 SyncRetentionOptions 类型。
/// </summary>
public class SyncRetentionOptions
{
    /// <summary>
    /// 获取或设置 Enabled。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 获取或设置 KeepMonths。
    /// </summary>
    public int KeepMonths { get; set; } = 3;

    /// <summary>
    /// 获取或设置 DryRun。
    /// </summary>
    public bool DryRun { get; set; } = true;

    /// <summary>
    /// 获取或设置 AllowDrop。
    /// </summary>
    public bool AllowDrop { get; set; }
}

