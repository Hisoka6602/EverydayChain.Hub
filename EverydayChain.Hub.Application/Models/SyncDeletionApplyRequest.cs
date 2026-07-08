using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 SyncDeletionApplyRequest 类型。
/// </summary>
public class SyncDeletionApplyRequest
{
    /// <summary>
    /// 获取或设置 TableCode。
    /// </summary>
    public string TableCode { get; set; } = string.Empty;

    public IReadOnlyList<string> BusinessKeys { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 获取或设置 DeletionPolicy。
    /// </summary>
    public DeletionPolicy DeletionPolicy { get; set; } = DeletionPolicy.Disabled;

    /// <summary>
    /// 获取或设置 DryRun。
    /// </summary>
    public bool DryRun { get; set; }
}

