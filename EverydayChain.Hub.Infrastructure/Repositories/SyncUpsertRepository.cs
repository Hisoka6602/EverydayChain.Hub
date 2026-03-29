using System.Collections.Concurrent;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Repositories;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 同步幂等合并仓储基础实现（内存幂等仓）。
/// </summary>
public class SyncUpsertRepository : ISyncUpsertRepository
{
    /// <summary>目标内存表，按表编码分组。</summary>
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, IReadOnlyDictionary<string, object?>>> _targetTables = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public Task<SyncMergeResult> MergeFromStagingAsync(SyncMergeRequest request, CancellationToken ct)
    {
        if (request.UniqueKeys.Count == 0)
        {
            throw new InvalidOperationException($"同步表 {request.TableCode} 未配置 UniqueKeys，无法执行幂等合并。");
        }

        var targetTable = _targetTables.GetOrAdd(request.TableCode, _ => new ConcurrentDictionary<string, IReadOnlyDictionary<string, object?>>());
        var changedOperations = new Dictionary<string, SyncChangeOperationType>(StringComparer.OrdinalIgnoreCase);
        var result = new SyncMergeResult
        {
            ChangedOperations = changedOperations,
        };

        foreach (var row in request.Rows)
        {
            ct.ThrowIfCancellationRequested();
            var filteredRow = SyncColumnFilter.FilterExcludedColumns(row, request.NormalizedExcludedColumns);

            var rowKey = SyncBusinessKeyBuilder.Build(filteredRow, request.UniqueKeys);
            if (string.IsNullOrWhiteSpace(rowKey))
            {
                continue;
            }

            if (!targetTable.TryGetValue(rowKey, out var existedRow))
            {
                targetTable[rowKey] = CloneRow(filteredRow);
                result.InsertCount++;
                changedOperations[rowKey] = SyncChangeOperationType.Insert;
                UpdateLastCursor(result, filteredRow, request.CursorColumn);
                continue;
            }

            if (AreRowsEqual(existedRow, filteredRow))
            {
                result.SkipCount++;
                UpdateLastCursor(result, filteredRow, request.CursorColumn);
                continue;
            }

            targetTable[rowKey] = CloneRow(filteredRow);
            result.UpdateCount++;
            changedOperations[rowKey] = SyncChangeOperationType.Update;
            UpdateLastCursor(result, filteredRow, request.CursorColumn);
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ListTargetRowsAsync(string tableCode, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!_targetTables.TryGetValue(tableCode, out var table))
        {
            return Task.FromResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>([]);
        }

        var rows = table.Values.ToList();
        return Task.FromResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>(rows);
    }

    /// <inheritdoc/>
    public Task<int> DeleteByBusinessKeysAsync(string tableCode, IReadOnlyList<string> businessKeys, DeletionPolicy deletionPolicy, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!_targetTables.TryGetValue(tableCode, out var table))
        {
            return Task.FromResult(0);
        }

        var deletedCount = 0;
        foreach (var businessKey in businessKeys)
        {
            ct.ThrowIfCancellationRequested();
            if (!table.TryGetValue(businessKey, out var row))
            {
                continue;
            }

            if (deletionPolicy == DeletionPolicy.SoftDelete)
            {
                var softDeletedRow = new Dictionary<string, object?>(row)
                {
                    [SyncColumnFilter.SoftDeleteFlagColumn] = true,
                    [SyncColumnFilter.SoftDeleteTimeColumn] = DateTime.Now,
                };
                table[businessKey] = softDeletedRow;
                deletedCount++;
                continue;
            }

            if (deletionPolicy == DeletionPolicy.HardDelete && table.TryRemove(businessKey, out _))
            {
                deletedCount++;
            }
        }

        return Task.FromResult(deletedCount);
    }

    /// <inheritdoc/>
    public string BuildBusinessKey(IReadOnlyDictionary<string, object?> row, IReadOnlyList<string> uniqueKeys)
    {
        return SyncBusinessKeyBuilder.Build(row, uniqueKeys);
    }

    /// <summary>
    /// 判断两行是否一致。
    /// </summary>
    /// <param name="left">旧值。</param>
    /// <param name="right">新值。</param>
    /// <returns>一致返回 <c>true</c>。</returns>
    private static bool AreRowsEqual(IReadOnlyDictionary<string, object?> left, IReadOnlyDictionary<string, object?> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (var pair in left)
        {
            if (!right.TryGetValue(pair.Key, out var rightValue))
            {
                return false;
            }

            if (!Equals(pair.Value, rightValue))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 更新最大成功游标。
    /// </summary>
    /// <param name="result">合并结果。</param>
    /// <param name="row">数据行。</param>
    /// <param name="cursorColumn">游标列。</param>
    private static void UpdateLastCursor(SyncMergeResult result, IReadOnlyDictionary<string, object?> row, string cursorColumn)
    {
        if (!row.TryGetValue(cursorColumn, out var value) || value is not DateTime cursorLocal)
        {
            return;
        }

        if (!result.LastSuccessCursorLocal.HasValue || cursorLocal > result.LastSuccessCursorLocal.Value)
        {
            result.LastSuccessCursorLocal = cursorLocal;
        }
    }

    /// <summary>
    /// 克隆行数据。
    /// </summary>
    /// <param name="row">原始行。</param>
    /// <returns>克隆结果。</returns>
    private static IReadOnlyDictionary<string, object?> CloneRow(IReadOnlyDictionary<string, object?> row)
    {
        return new Dictionary<string, object?>(row);
    }

}
