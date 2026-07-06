namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface ISyncStagingRepository
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task BulkInsertAsync(string batchId, int pageNo, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, IReadOnlySet<string> normalizedExcludedColumns, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetPageRowsAsync(string batchId, int pageNo, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task ClearPageAsync(string batchId, int pageNo, CancellationToken ct);
}

