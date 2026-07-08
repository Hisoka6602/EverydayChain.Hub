using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义 ISyncUpsertRepository 类型。
/// </summary>
public interface ISyncUpsertRepository
{
    /// <summary>
    /// 执行 MergeFromStagingAsync 方法。
    /// </summary>
    Task<SyncMergeResult> MergeFromStagingAsync(SyncMergeRequest request, CancellationToken ct);

    /// <summary>
    /// 执行 ListTargetStateRowsAsync 方法。
    /// </summary>
    Task<IReadOnlyList<SyncTargetStateRow>> ListTargetStateRowsAsync(string tableCode, CancellationToken ct);

    /// <summary>
    /// 执行 DeleteByBusinessKeysAsync 方法。
    /// </summary>
    Task<int> DeleteByBusinessKeysAsync(string tableCode, IReadOnlyList<string> businessKeys, DeletionPolicy deletionPolicy, CancellationToken ct);

}

