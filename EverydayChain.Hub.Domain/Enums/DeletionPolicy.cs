using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 定义 DeletionPolicy 类型。
/// </summary>
public enum DeletionPolicy
{
    /// <summary>
    /// 表示关闭删除同步。
    /// </summary>
    [Description("关闭删除同步")]
    Disabled = 1,

    /// <summary>
    /// 表示通过标记方式执行软删除。
    /// </summary>
    [Description("软删除")]
    SoftDelete = 2,

    /// <summary>
    /// 表示直接删除目标数据。
    /// </summary>
    [Description("硬删除")]
    HardDelete = 3,
}

