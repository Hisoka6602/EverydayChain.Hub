using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义 ISyncDeletionRepository 类型。
/// </summary>
public interface ISyncDeletionRepository
{
    /// <summary>
    /// 执行 DetectDeletedKeysAsync 方法。
    /// </summary>
    Task<IReadOnlyList<SyncDeletionCandidate>> DetectDeletedKeysAsync(SyncDeletionDetectRequest request, CancellationToken ct);

    /// <summary>
    /// 执行 ApplyDeletionAsync 方法。
    /// </summary>
    Task<int> ApplyDeletionAsync(SyncDeletionApplyRequest request, CancellationToken ct);
}

