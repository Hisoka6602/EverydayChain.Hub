using System.Collections.Concurrent;
using System.Globalization;
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
    /// <summary>目标端持久化文件目录。</summary>
    private readonly string _targetStoreDirectoryPath = ResolveTargetStoreDirectoryPath(ResolveTargetStoreFilePath(syncJobOptions.Value.TargetStoreFilePath));
    /// <summary>目标端持久化文件名前缀。</summary>
    private readonly string _targetStoreFileNamePrefix = ResolveTargetStoreFileNamePrefix(ResolveTargetStoreFilePath(syncJobOptions.Value.TargetStoreFilePath));
    /// <summary>目标端持久化文件扩展名。</summary>
    private readonly string _targetStoreFileNameExtension = ResolveTargetStoreFileNameExtension(ResolveTargetStoreFilePath(syncJobOptions.Value.TargetStoreFilePath));

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

    /// <summary>
    /// 解析目标端持久化文件目录。
    /// </summary>
    /// <param name="targetStoreFilePath">目标端持久化文件路径。</param>
    /// <returns>目录绝对路径。</returns>
    private static string ResolveTargetStoreDirectoryPath(string targetStoreFilePath)
    {
        return Path.GetDirectoryName(targetStoreFilePath) ?? AppContext.BaseDirectory;
    }

    /// <summary>
    /// 解析目标端持久化文件名前缀。
    /// </summary>
    /// <param name="targetStoreFilePath">目标端持久化文件路径。</param>
    /// <returns>文件名前缀。</returns>
    private static string ResolveTargetStoreFileNamePrefix(string targetStoreFilePath)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(targetStoreFilePath);
        return string.IsNullOrWhiteSpace(fileNameWithoutExtension) ? "sync-target-store" : fileNameWithoutExtension;
    }

    /// <summary>
    /// 解析目标端持久化文件扩展名。
    /// </summary>
    /// <param name="targetStoreFilePath">目标端持久化文件路径。</param>
    /// <returns>文件扩展名。</returns>
    private static string ResolveTargetStoreFileNameExtension(string targetStoreFilePath)
    {
        var extension = Path.GetExtension(targetStoreFilePath);
        return string.IsNullOrWhiteSpace(extension) ? ".json" : extension;
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
        var hasChanges = false;

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
                hasChanges = true;
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
            hasChanges = true;
        }

        if (hasChanges)
        {
            await PersistTableAsync(request.TableCode, targetTable, ct);
        }

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

        if (deletedCount > 0)
        {
            await PersistTableAsync(tableCode, table, ct);
        }

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

            var targetTable = CreateBusinessKeyDictionary();
            var tableRows = await LoadTableRowsWithoutLockAsync(tableCode, ct);
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
        var persistedTable = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in table)
        {
            persistedTable[pair.Key] = new Dictionary<string, object?>(pair.Value);
        }

        await TargetStoreFileLock.WaitAsync(ct);
        try
        {
            await SaveTableRowsWithoutLockAsync(tableCode, persistedTable, ct);
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
    /// 读取指定表持久化数据（调用方需保证已持锁）。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>指定表持久化数据。</returns>
    private async Task<Dictionary<string, Dictionary<string, object?>>> LoadTableRowsWithoutLockAsync(string tableCode, CancellationToken ct)
    {
        var tableStoreFilePath = BuildPerTableStoreFilePath(tableCode);
        if (File.Exists(tableStoreFilePath))
        {
            var tableJson = await File.ReadAllTextAsync(tableStoreFilePath, ct);
            if (string.IsNullOrWhiteSpace(tableJson))
            {
                return new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
            }

            return DeserializeTableRows(tableJson, tableStoreFilePath, tableCode);
        }

        if (!File.Exists(_targetStoreFilePath))
        {
            return new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        }

        // 兼容旧版全量文件落地格式，迁移后新写入统一落到按表文件。
        var json = await File.ReadAllTextAsync(_targetStoreFilePath, ct);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var deserialized = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, object?>>>>(json)
                ?? new Dictionary<string, Dictionary<string, Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase);
            if (!deserialized.TryGetValue(tableCode, out var legacyRows))
            {
                return new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
            }

            return NormalizeTableRows(legacyRows);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "读取同步目标落地文件失败。Path={TargetStoreFilePath}, TableCode={TableCode}", _targetStoreFilePath, tableCode);
            throw new InvalidOperationException(
                $"读取同步目标落地文件失败（Path={_targetStoreFilePath}, TableCode={tableCode}）。请检查文件内容是否为有效 JSON，必要时备份后清理该运行期文件再重试。",
                ex);
        }
    }

    /// <summary>
    /// 保存指定表持久化数据（调用方需保证已持锁）。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    /// <param name="tableRows">指定表数据。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task SaveTableRowsWithoutLockAsync(string tableCode, Dictionary<string, Dictionary<string, object?>> tableRows, CancellationToken ct)
    {
        if (!Directory.Exists(_targetStoreDirectoryPath))
        {
            Directory.CreateDirectory(_targetStoreDirectoryPath);
        }

        var json = JsonSerializer.Serialize(tableRows, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
        var tableStoreFilePath = BuildPerTableStoreFilePath(tableCode);
        var tempFilePath = $"{tableStoreFilePath}.tmp";
        var backupFilePath = $"{tableStoreFilePath}.bak";
        try
        {
            await File.WriteAllTextAsync(tempFilePath, json, ct);
            if (File.Exists(tableStoreFilePath))
            {
                File.Replace(tempFilePath, tableStoreFilePath, backupFilePath, true);
                if (File.Exists(backupFilePath))
                {
                    File.Delete(backupFilePath);
                }
            }
            else
            {
                File.Move(tempFilePath, tableStoreFilePath);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "原子写入同步目标落地文件失败。Path={TargetStoreFilePath}, TableCode={TableCode}", tableStoreFilePath, tableCode);
            throw new InvalidOperationException(
                $"原子写入同步目标落地文件失败（Path={tableStoreFilePath}, TableCode={tableCode}）。请检查目录权限与磁盘空间后重试。",
                ex);
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    /// <summary>
    /// 构建按表落地文件路径。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    /// <returns>按表落地文件绝对路径。</returns>
    private string BuildPerTableStoreFilePath(string tableCode)
    {
        var safeTableCode = tableCode;
        foreach (var invalidFileNameChar in Path.GetInvalidFileNameChars())
        {
            safeTableCode = safeTableCode.Replace(invalidFileNameChar, '_');
        }

        var fileName = $"{_targetStoreFileNamePrefix}.{safeTableCode}{_targetStoreFileNameExtension}";
        return Path.Combine(_targetStoreDirectoryPath, fileName);
    }

    /// <summary>
    /// 反序列化按表持久化数据并恢复字段类型。
    /// </summary>
    /// <param name="json">JSON 文本。</param>
    /// <param name="path">文件路径。</param>
    /// <param name="tableCode">表编码。</param>
    /// <returns>按表数据。</returns>
    private Dictionary<string, Dictionary<string, object?>> DeserializeTableRows(string json, string path, string tableCode)
    {
        try
        {
            var deserialized = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object?>>>(json)
                ?? new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
            return NormalizeTableRows(deserialized);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "读取同步目标落地文件失败。Path={TargetStoreFilePath}, TableCode={TableCode}", path, tableCode);
            throw new InvalidOperationException(
                $"读取同步目标落地文件失败（Path={path}, TableCode={tableCode}）。请检查文件内容是否为有效 JSON，必要时备份后清理该运行期文件再重试。",
                ex);
        }
    }

    /// <summary>
    /// 归一化按表数据，避免 JsonElement 影响后续比较与游标计算。
    /// </summary>
    /// <param name="rows">按表数据。</param>
    /// <returns>归一化结果。</returns>
    private static Dictionary<string, Dictionary<string, object?>> NormalizeTableRows(Dictionary<string, Dictionary<string, object?>> rows)
    {
        var normalized = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in rows)
        {
            normalized[pair.Key] = ConvertPersistedRow(pair.Value);
        }

        return normalized;
    }

    /// <summary>
    /// 将单行持久化数据转换为可比较的 CLR 类型。
    /// </summary>
    /// <param name="row">单行数据。</param>
    /// <returns>转换后的行。</returns>
    private static Dictionary<string, object?> ConvertPersistedRow(Dictionary<string, object?> row)
    {
        var converted = new Dictionary<string, object?>(row.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in row)
        {
            converted[pair.Key] = pair.Value is JsonElement element ? ConvertJsonElement(element) : pair.Value;
        }

        return converted;
    }

    /// <summary>
    /// 将 JsonElement 转换为常见 CLR 类型。
    /// </summary>
    /// <param name="element">JSON 值。</param>
    /// <returns>转换结果。</returns>
    private static object? ConvertJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var longValue))
                {
                    return longValue;
                }

                if (element.TryGetDecimal(out var decimalValue))
                {
                    return decimalValue;
                }

                return element.GetRawText();
            case JsonValueKind.String:
                if (element.TryGetDateTime(out var dateTimeValue))
                {
                    return EnsureLocalDateTime(dateTimeValue, element.GetString());
                }

                var stringValue = element.GetString();
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return stringValue;
                }

                if (ContainsOffsetOrZulu(stringValue))
                {
                    throw new InvalidOperationException($"不支持包含 Z 或 offset 的时间文本：{stringValue}");
                }

                if (DateTime.TryParse(
                        stringValue,
                        CultureInfo.CurrentCulture,
                        DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces,
                        out var parsedLocalDateTime))
                {
                    return EnsureLocalDateTime(parsedLocalDateTime, stringValue);
                }

                return stringValue;
            default:
                return element.GetRawText();
        }
    }

    /// <summary>
    /// 确保时间值满足本地时间语义。
    /// </summary>
    /// <param name="value">时间值。</param>
    /// <param name="originalText">原始文本。</param>
    /// <returns>本地语义时间值。</returns>
    private static DateTime EnsureLocalDateTime(DateTime value, string? originalText)
    {
        if (value.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Local);
        }

        if (value.Kind == DateTimeKind.Local)
        {
            return value;
        }

        throw new InvalidOperationException($"检测到非本地时间语义，已拒绝加载：{originalText ?? value.ToString("O")}");
    }

    /// <summary>
    /// 判断时间文本是否包含 Z 或 offset 信息。
    /// </summary>
    /// <param name="value">时间文本。</param>
    /// <returns>包含则返回 <c>true</c>。</returns>
    private static bool ContainsOffsetOrZulu(string value)
    {
        if (value.EndsWith('Z') || value.EndsWith('z'))
        {
            return true;
        }

        var separatorIndex = value.IndexOf('T');
        if (separatorIndex < 0)
        {
            separatorIndex = value.IndexOf(' ');
        }

        if (separatorIndex < 0 || separatorIndex >= value.Length - 1)
        {
            return false;
        }

        var timePart = value[(separatorIndex + 1)..];
        return timePart.Contains('+') || timePart.Contains('-');
    }
}
