namespace EverydayChain.Hub.Domain.Runtime;

/// <summary>
/// 表示运行时租约的只读快照。
/// </summary>
public sealed class RuntimeLeaseSnapshot
{
    /// <summary>
    /// 获取或设置租约键。
    /// </summary>
    public string LeaseKey { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置持有者标识。
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置获取时间。
    /// </summary>
    public DateTime AcquiredTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置过期时间。
    /// </summary>
    public DateTime ExpiresAtLocal { get; set; }
}
