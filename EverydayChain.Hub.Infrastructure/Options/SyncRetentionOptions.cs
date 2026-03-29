namespace EverydayChain.Hub.Infrastructure.Options;

/// <summary>
/// 单表保留期治理配置。
/// </summary>
public class SyncRetentionOptions
{
    /// <summary>是否启用保留期治理。</summary>
    public bool Enabled { get; set; }

    /// <summary>保留最近月份数（最小为 1，默认 3）。</summary>
    public int KeepMonths { get; set; } = 3;

    /// <summary>保留期清理是否仅预演（仅审计，不执行）。</summary>
    public bool DryRun { get; set; } = true;

    /// <summary>是否允许执行删除分表动作。</summary>
    public bool AllowDrop { get; set; }
}
