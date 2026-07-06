using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncDeletionApplyRequest
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string TableCode { get; set; } = string.Empty;

    public IReadOnlyList<string> BusinessKeys { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DeletionPolicy DeletionPolicy { get; set; } = DeletionPolicy.Disabled;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool DryRun { get; set; }
}

