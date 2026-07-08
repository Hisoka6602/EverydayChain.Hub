namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义 ISyncStagingRepository 类型。
/// </summary>
public interface ISyncStagingRepository
{
    /// <summary>
    /// 执行 BulkInsertAsync 方法。
    /// </summary>
    Task BulkInsertAsync(string batchId, int pageNo, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, IReadOnlySet<string> normalizedExcludedColumns, CancellationToken ct);

    /// <summary>
    /// 执行 GetPageRowsAsync 方法。
    /// </summary>
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetPageRowsAsync(string batchId, int pageNo, CancellationToken ct);

    /// <summary>
    /// 执行 ClearPageAsync 方法。
    /// </summary>
    Task ClearPageAsync(string batchId, int pageNo, CancellationToken ct);
}

