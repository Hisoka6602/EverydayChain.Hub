namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义运行时租约仓储契约。
/// </summary>
public interface IRuntimeLeaseRepository
{
    /// <summary>
    /// 尝试获取指定租约。
    /// </summary>
    /// <param name="leaseKey">租约键。</param>
    /// <param name="ownerId">持有者标识。</param>
    /// <param name="acquiredTimeLocal">获取时间。</param>
    /// <param name="expiresAtLocal">过期时间。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>获取成功返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    Task<bool> TryAcquireAsync(
        string leaseKey,
        string ownerId,
        DateTime acquiredTimeLocal,
        DateTime expiresAtLocal,
        CancellationToken ct);

    /// <summary>
    /// 释放指定租约。
    /// </summary>
    /// <param name="leaseKey">租约键。</param>
    /// <param name="ownerId">持有者标识。</param>
    /// <param name="ct">取消令牌。</param>
    Task ReleaseAsync(string leaseKey, string ownerId, CancellationToken ct);
}
