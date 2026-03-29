using System.Collections.Concurrent;
using EverydayChain.Hub.Application.Repositories;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 同步暂存仓储基础实现（内存暂存）。
/// </summary>
public class SyncStagingRepository : ISyncStagingRepository
{
    /// <summary>暂存字典，键格式为 <c>{batchId}:{pageNo}</c>。</summary>
    private readonly ConcurrentDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>> _staging = new();

    /// <inheritdoc/>
    public Task BulkInsertAsync(string batchId, int pageNo, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, IReadOnlyList<string> excludedColumns, CancellationToken ct)
    {
        var storageKey = BuildStorageKey(batchId, pageNo);
        _staging[storageKey] = rows.Select(row => FilterExcludedColumns(row, excludedColumns)).ToList();
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

    /// <summary>
    /// 过滤行中的排除列。
    /// </summary>
    /// <param name="row">原始数据行。</param>
    /// <param name="excludedColumns">排除列集合。</param>
    /// <returns>过滤后的数据行。</returns>
    private static IReadOnlyDictionary<string, object?> FilterExcludedColumns(IReadOnlyDictionary<string, object?> row, IReadOnlyList<string> excludedColumns)
    {
        if (excludedColumns.Count == 0)
        {
            return new Dictionary<string, object?>(row);
        }

        var excludedColumnSet = excludedColumns
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(static x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (excludedColumnSet.Count == 0)
        {
            return new Dictionary<string, object?>(row);
        }

        var filtered = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in row)
        {
            if (!excludedColumnSet.Contains(pair.Key))
            {
                filtered[pair.Key] = pair.Value;
            }
        }

        return filtered;
    }
}
