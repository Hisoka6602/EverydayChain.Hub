using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 单表删除同步配置。
/// </summary>
public class SyncDeleteOptions
{
    /// <summary>删除策略（Disabled/SoftDelete/HardDelete）。</summary>
    public DeletionPolicy Policy { get; set; } = DeletionPolicy.Disabled;

    /// <summary>是否启用删除同步。</summary>
    public bool Enabled { get; set; }

    /// <summary>是否预演删除（仅审计，不执行）。</summary>
    public bool DryRun { get; set; }

    /// <summary>删除比对分段大小。</summary>
    public int CompareSegmentSize { get; set; } = 20000;

    /// <summary>删除比对最大并行度。</summary>
    public int CompareMaxParallelism { get; set; } = 4;
}
