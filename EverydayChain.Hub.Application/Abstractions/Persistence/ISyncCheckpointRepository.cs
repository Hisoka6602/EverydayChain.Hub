using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface ISyncCheckpointRepository
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<SyncCheckpoint> GetAsync(string tableCode, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task SaveAsync(SyncCheckpoint checkpoint, CancellationToken ct);
}

