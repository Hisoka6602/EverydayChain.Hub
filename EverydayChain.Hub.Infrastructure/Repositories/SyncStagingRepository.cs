using System.Collections.Concurrent;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.SharedKernel.Utilities;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncStagingRepository : ISyncStagingRepository
{
    private readonly ConcurrentDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>> _staging = new();

    public Task BulkInsertAsync(string batchId, int pageNo, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, IReadOnlySet<string> normalizedExcludedColumns, CancellationToken ct)
    {
        var storageKey = BuildStorageKey(batchId, pageNo);
        var pageRows = rows
            .Select(row =>
            {
                var filteredRow = SyncColumnFilter.FilterExcludedColumns(row, normalizedExcludedColumns);
                return new Dictionary<string, object?>(filteredRow, StringComparer.OrdinalIgnoreCase);
            })
            .ToList();
        _staging[storageKey] = pageRows;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetPageRowsAsync(string batchId, int pageNo, CancellationToken ct)
    {
        var storageKey = BuildStorageKey(batchId, pageNo);
        if (_staging.TryGetValue(storageKey, out var rows))
        {
            return Task.FromResult(rows);
        }

        return Task.FromResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>(Array.Empty<IReadOnlyDictionary<string, object?>>());
    }

    public Task ClearPageAsync(string batchId, int pageNo, CancellationToken ct)
    {
        var storageKey = BuildStorageKey(batchId, pageNo);
        _staging.TryRemove(storageKey, out _);
        return Task.CompletedTask;
    }

    private static string BuildStorageKey(string batchId, int pageNo)
    {
        return $"{batchId}:{pageNo}";
    }

}

