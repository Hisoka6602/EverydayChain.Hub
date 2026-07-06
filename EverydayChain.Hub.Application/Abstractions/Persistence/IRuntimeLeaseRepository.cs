namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IRuntimeLeaseRepository
{
    Task<bool> TryAcquireAsync(
        string leaseKey,
        string ownerId,
        DateTime acquiredTimeLocal,
        DateTime expiresAtLocal,
        CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task ReleaseAsync(string leaseKey, string ownerId, CancellationToken ct);
}

