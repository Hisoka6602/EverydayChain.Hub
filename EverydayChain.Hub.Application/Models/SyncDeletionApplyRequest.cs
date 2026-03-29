using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 删除执行请求。
/// </summary>
public class SyncDeletionApplyRequest
{
    /// <summary>表编码。</summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>待删除业务键集合。</summary>
    public IReadOnlyList<string> BusinessKeys { get; set; } = Array.Empty<string>();

    /// <summary>删除策略。</summary>
    public DeletionPolicy DeletionPolicy { get; set; } = DeletionPolicy.Disabled;

    /// <summary>是否仅预演（仅审计，不执行）。</summary>
    public bool DryRun { get; set; }
}
