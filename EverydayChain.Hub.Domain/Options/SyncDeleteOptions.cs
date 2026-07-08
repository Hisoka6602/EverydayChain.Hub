using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义 SyncDeleteOptions 类型。
/// </summary>
public class SyncDeleteOptions
{
    /// <summary>
    /// 获取或设置 Policy。
    /// </summary>
    public DeletionPolicy Policy { get; set; } = DeletionPolicy.Disabled;

    /// <summary>
    /// 获取或设置 Enabled。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 获取或设置 DryRun。
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// 获取或设置 CompareSegmentSize。
    /// </summary>
    public int CompareSegmentSize { get; set; } = 20000;

    /// <summary>
    /// 获取或设置 CompareMaxParallelism。
    /// </summary>
    public int CompareMaxParallelism { get; set; } = 4;
}

