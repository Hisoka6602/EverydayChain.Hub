namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncDeletionCandidate
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string BusinessKey { get; set; } = string.Empty;

    public IReadOnlyDictionary<string, object?> TargetSnapshot { get; set; } = new Dictionary<string, object?>();

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string SourceEvidence { get; set; } = string.Empty;
}

