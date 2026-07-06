using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncDeleteOptions
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DeletionPolicy Policy { get; set; } = DeletionPolicy.Disabled;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int CompareSegmentSize { get; set; } = 20000;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int CompareMaxParallelism { get; set; } = 4;
}

