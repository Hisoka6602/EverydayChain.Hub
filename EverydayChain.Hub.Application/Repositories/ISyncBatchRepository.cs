using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Repositories;

/// <summary>
/// 同步批次仓储接口。
/// </summary>
public interface ISyncBatchRepository
{
    /// <summary>
    /// 创建批次记录（Pending）。
    /// </summary>
    /// <param name="batch">批次信息。</param>
    /// <param name="ct">取消令牌。</param>
    Task CreateBatchAsync(SyncBatch batch, CancellationToken ct);

    /// <summary>
    /// 标记批次进入执行中（InProgress）。
    /// </summary>
    /// <param name="batchId">批次编号。</param>
    /// <param name="startedTimeLocal">开始时间（本地）。</param>
    /// <param name="ct">取消令牌。</param>
    Task MarkInProgressAsync(string batchId, DateTime startedTimeLocal, CancellationToken ct);

    /// <summary>
    /// 标记批次完成（Completed）。
    /// </summary>
    /// <param name="result">批次结果。</param>
    /// <param name="completedTimeLocal">完成时间（本地）。</param>
    /// <param name="ct">取消令牌。</param>
    Task CompleteBatchAsync(SyncBatchResult result, DateTime completedTimeLocal, CancellationToken ct);

    /// <summary>
    /// 标记批次失败（Failed）。
    /// </summary>
    /// <param name="batchId">批次编号。</param>
    /// <param name="errorMessage">错误信息。</param>
    /// <param name="failedTimeLocal">失败时间（本地）。</param>
    /// <param name="ct">取消令牌。</param>
    Task FailBatchAsync(string batchId, string errorMessage, DateTime failedTimeLocal, CancellationToken ct);

    /// <summary>
    /// 获取最近失败批次编号（用于重试关联）。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>批次编号。</returns>
    Task<string?> GetLatestFailedBatchIdAsync(string tableCode, CancellationToken ct);
}
