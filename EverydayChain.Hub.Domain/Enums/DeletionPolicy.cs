using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 定义当前类型。
/// </summary>
public enum DeletionPolicy
{
    [Description("关闭删除同步")]
    Disabled = 1,

    [Description("软删除")]
    SoftDelete = 2,

    [Description("硬删除")]
    HardDelete = 3,
}

