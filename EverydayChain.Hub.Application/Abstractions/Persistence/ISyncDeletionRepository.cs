using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 同步删除仓储接口。
/// </summary>
public interface ISyncDeletionRepository
{
    /// <summary>
    /// 识别待删除业务键集合。
    /// </summary>
    /// <param name="request">删除识别请求。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>删除候选集合。</returns>
    Task<IReadOnlyList<SyncDeletionCandidate>> DetectDeletedKeysAsync(SyncDeletionDetectRequest request, CancellationToken ct);

    /// <summary>
    /// 执行删除动作。
    /// </summary>
    /// <param name="request">删除执行请求。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>实际执行删除数量。</returns>
    Task<int> ApplyDeletionAsync(SyncDeletionApplyRequest request, CancellationToken ct);
}
