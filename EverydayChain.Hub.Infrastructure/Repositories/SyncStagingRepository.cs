using System.Collections.Concurrent;
using EverydayChain.Hub.Application.Repositories;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 同步暂存仓储基础实现（内存暂存）。
/// </summary>
public class SyncStagingRepository : ISyncStagingRepository
{
    /// <summary>暂存字典，键格式为 <c>{batchId}:{pageNo}</c>。</summary>
    private readonly ConcurrentDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>> _staging = new();

    /// <inheritdoc/>
    public Task BulkInsertAsync(string batchId, int pageNo, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, IReadOnlySet<string> normalizedExcludedColumns, CancellationToken ct)
    {
        var storageKey = BuildStorageKey(batchId, pageNo);
        _staging[storageKey] = rows.Select(row => SyncColumnFilter.FilterExcludedColumns(row, normalizedExcludedColumns)).ToList();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetPageRowsAsync(string batchId, int pageNo, CancellationToken ct)
    {
        var storageKey = BuildStorageKey(batchId, pageNo);
        if (_staging.TryGetValue(storageKey, out var rows))
        {
            return Task.FromResult(rows);
        }

        return Task.FromResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>(Array.Empty<IReadOnlyDictionary<string, object?>>());
    }

    /// <inheritdoc/>
    public Task ClearPageAsync(string batchId, int pageNo, CancellationToken ct)
    {
        var storageKey = BuildStorageKey(batchId, pageNo);
        _staging.TryRemove(storageKey, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 生成暂存键。
    /// </summary>
    /// <param name="batchId">批次编号。</param>
    /// <param name="pageNo">页码。</param>
    /// <returns>暂存键。</returns>
    private static string BuildStorageKey(string batchId, int pageNo)
    {
        return $"{batchId}:{pageNo}";
    }

}
