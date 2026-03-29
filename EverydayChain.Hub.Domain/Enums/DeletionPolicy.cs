using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 删除策略。
/// </summary>
public enum DeletionPolicy
{
    /// <summary>
    /// 关闭删除同步。
    /// </summary>
    [Description("关闭删除同步")]
    Disabled = 1,

    /// <summary>
    /// 软删除。
    /// </summary>
    [Description("软删除")]
    SoftDelete = 2,

    /// <summary>
    /// 硬删除。
    /// </summary>
    [Description("硬删除")]
    HardDelete = 3,
}
