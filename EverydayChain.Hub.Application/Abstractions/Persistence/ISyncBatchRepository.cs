using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface ISyncBatchRepository
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task CreateBatchAsync(SyncBatch batch, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task MarkInProgressAsync(string batchId, DateTime startedTimeLocal, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task CompleteBatchAsync(SyncBatchResult result, DateTime completedTimeLocal, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task FailBatchAsync(string batchId, string errorMessage, DateTime failedTimeLocal, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<string?> GetLatestFailedBatchIdAsync(string tableCode, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<IReadOnlyList<SyncBatch>> ListLatestByTableCodesAsync(IReadOnlyCollection<string> tableCodes, CancellationToken ct);
}

