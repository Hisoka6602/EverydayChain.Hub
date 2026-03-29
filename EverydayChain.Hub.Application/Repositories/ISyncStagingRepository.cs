namespace EverydayChain.Hub.Application.Repositories;

/// <summary>
/// 同步暂存仓储接口。
/// </summary>
public interface ISyncStagingRepository
{
    /// <summary>
    /// 批量写入暂存。
    /// </summary>
    /// <param name="batchId">批次编号。</param>
    /// <param name="pageNo">页码。</param>
    /// <param name="rows">数据行。</param>
    /// <param name="ct">取消令牌。</param>
    Task BulkInsertAsync(string batchId, int pageNo, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, CancellationToken ct);

    /// <summary>
    /// 读取暂存页数据。
    /// </summary>
    /// <param name="batchId">批次编号。</param>
    /// <param name="pageNo">页码。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>暂存行数据。</returns>
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetPageRowsAsync(string batchId, int pageNo, CancellationToken ct);

    /// <summary>
    /// 清理暂存页数据。
    /// </summary>
    /// <param name="batchId">批次编号。</param>
    /// <param name="pageNo">页码。</param>
    /// <param name="ct">取消令牌。</param>
    Task ClearPageAsync(string batchId, int pageNo, CancellationToken ct);
}
