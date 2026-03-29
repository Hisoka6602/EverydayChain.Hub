using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Repositories;

/// <summary>
/// 同步检查点仓储接口。
/// </summary>
public interface ISyncCheckpointRepository
{
    /// <summary>
    /// 获取检查点。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>检查点对象。</returns>
    Task<SyncCheckpoint> GetAsync(string tableCode, CancellationToken ct);

    /// <summary>
    /// 保存检查点。
    /// </summary>
    /// <param name="checkpoint">检查点对象。</param>
    /// <param name="ct">取消令牌。</param>
    Task SaveAsync(SyncCheckpoint checkpoint, CancellationToken ct);
}
