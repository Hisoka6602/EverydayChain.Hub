namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 删除差异候选项。
/// </summary>
public class SyncDeletionCandidate
{
    /// <summary>业务键。</summary>
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>目标端删除前快照。</summary>
    public IReadOnlyDictionary<string, object?> TargetSnapshot { get; set; } = new Dictionary<string, object?>();

    /// <summary>源端缺失证据文本。</summary>
    public string SourceEvidence { get; set; } = string.Empty;
}
