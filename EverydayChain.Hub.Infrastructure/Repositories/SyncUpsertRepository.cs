using System.Collections.Concurrent;
using System.Text.Json;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Repositories;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 同步幂等合并仓储基础实现（内存幂等仓）。
/// </summary>
public class SyncUpsertRepository(IOptions<SyncJobOptions> syncJobOptions, ILogger<SyncUpsertRepository> logger) : ISyncUpsertRepository
{
    /// <summary>目标内存表，按表编码分组。</summary>
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, IReadOnlyDictionary<string, object?>>> _targetTables = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>目标端持久化文件访问锁。</summary>
    private static readonly SemaphoreSlim TargetStoreFileLock = new(1, 1);
    /// <summary>目标端持久化文件路径。</summary>
    private readonly string _targetStoreFilePath = ResolveTargetStoreFilePath(syncJobOptions.Value.TargetStoreFilePath);

    /// <summary>
    /// 解析目标端持久化文件路径。
    /// </summary>
    /// <param name="configuredPath">配置路径。</param>
    /// <returns>可用路径。</returns>
    private static string ResolveTargetStoreFilePath(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.Combine(AppContext.BaseDirectory, "data", "sync-target-store.json");
        }

        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, configuredPath);
    }

    /// <inheritdoc/>
    public async Task<SyncMergeResult> MergeFromStagingAsync(SyncMergeRequest request, CancellationToken ct)
    {
        if (request.UniqueKeys.Count == 0)
        {
            throw new InvalidOperationException($"同步表 {request.TableCode} 未配置 UniqueKeys，无法执行幂等合并。");
        }

        await EnsureTableLoadedAsync(request.TableCode, ct);
        var targetTable = _targetTables.GetOrAdd(request.TableCode, _ => CreateBusinessKeyDictionary());
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

        await PersistTableAsync(request.TableCode, targetTable, ct);
        return result;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ListTargetRowsAsync(string tableCode, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await EnsureTableLoadedAsync(tableCode, ct);
        if (!_targetTables.TryGetValue(tableCode, out var table))
        {
            return [];
        }

        var rows = table.Values.ToList();
        return rows;
    }

    /// <inheritdoc/>
    public async Task<int> DeleteByBusinessKeysAsync(string tableCode, IReadOnlyList<string> businessKeys, DeletionPolicy deletionPolicy, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await EnsureTableLoadedAsync(tableCode, ct);
        if (!_targetTables.TryGetValue(tableCode, out var table))
        {
            return 0;
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

        await PersistTableAsync(tableCode, table, ct);
        return deletedCount;
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

    /// <summary>
    /// 创建业务键字典（忽略大小写）。
    /// </summary>
    /// <returns>业务键字典。</returns>
    private static ConcurrentDictionary<string, IReadOnlyDictionary<string, object?>> CreateBusinessKeyDictionary()
    {
        return new ConcurrentDictionary<string, IReadOnlyDictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 确保表数据已从落地文件加载。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task EnsureTableLoadedAsync(string tableCode, CancellationToken ct)
    {
        if (_targetTables.ContainsKey(tableCode))
        {
            return;
        }

        await TargetStoreFileLock.WaitAsync(ct);
        try
        {
            if (_targetTables.ContainsKey(tableCode))
            {
                return;
            }

            var allTables = await LoadAllTablesWithoutLockAsync(ct);
            if (!allTables.TryGetValue(tableCode, out var tableRows))
            {
                _targetTables.TryAdd(tableCode, CreateBusinessKeyDictionary());
                return;
            }

            var targetTable = CreateBusinessKeyDictionary();
            foreach (var pair in tableRows)
            {
                targetTable[pair.Key] = CloneRow(pair.Value);
            }

            _targetTables.TryAdd(tableCode, targetTable);
        }
        finally
        {
            TargetStoreFileLock.Release();
        }
    }

    /// <summary>
    /// 将指定表持久化到目标端落地文件。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    /// <param name="table">目标表字典。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task PersistTableAsync(string tableCode, ConcurrentDictionary<string, IReadOnlyDictionary<string, object?>> table, CancellationToken ct)
    {
        await TargetStoreFileLock.WaitAsync(ct);
        try
        {
            var allTables = await LoadAllTablesWithoutLockAsync(ct);
            var persistedTable = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in table)
            {
                persistedTable[pair.Key] = new Dictionary<string, object?>(pair.Value);
            }

            allTables[tableCode] = persistedTable;
            await SaveAllTablesWithoutLockAsync(allTables, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "写入同步目标落地文件失败。Path={TargetStoreFilePath}, TableCode={TableCode}", _targetStoreFilePath, tableCode);
            throw;
        }
        finally
        {
            TargetStoreFileLock.Release();
        }
    }

    /// <summary>
    /// 读取全部目标表持久化数据（调用方需保证已持锁）。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>全部目标表数据。</returns>
    private async Task<Dictionary<string, Dictionary<string, Dictionary<string, object?>>>> LoadAllTablesWithoutLockAsync(CancellationToken ct)
    {
        if (!File.Exists(_targetStoreFilePath))
        {
            return new Dictionary<string, Dictionary<string, Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase);
        }

        var json = await File.ReadAllTextAsync(_targetStoreFilePath, ct);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, Dictionary<string, Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase);
        }

        var deserialized = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, object?>>>>(json)
            ?? new Dictionary<string, Dictionary<string, Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase);
        return new Dictionary<string, Dictionary<string, Dictionary<string, object?>>>(deserialized, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 保存全部目标表持久化数据（调用方需保证已持锁）。
    /// </summary>
    /// <param name="allTables">全部目标表数据。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task SaveAllTablesWithoutLockAsync(Dictionary<string, Dictionary<string, Dictionary<string, object?>>> allTables, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(_targetStoreFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(allTables, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
        await File.WriteAllTextAsync(_targetStoreFilePath, json, ct);
    }
}
