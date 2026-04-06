using System.Collections.Concurrent;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.SharedKernel.Utilities;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 同步幂等合并仓储基础实现（内存幂等仓）。
/// </summary>
public class SyncUpsertRepository : ISyncUpsertRepository
{
    /// <summary>行摘要序列化配置（紧凑输出）。</summary>
    private static readonly JsonSerializerOptions DigestSerializerOptions = new()
    {
        WriteIndented = false,
    };
    /// <summary>目标内存表，按表编码分组。</summary>
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, SyncTargetStateRow>> _targetTables = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>各表最后访问时间戳（Stopwatch.GetTimestamp()），用于空闲驱逐判定。</summary>
    private readonly ConcurrentDictionary<string, long> _tableLastAccessTimestamps = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>目标端持久化文件访问锁。</summary>
    private static readonly SemaphoreSlim TargetStoreFileLock = new(1, 1);
    /// <summary>日志组件。</summary>
    private readonly ILogger<SyncUpsertRepository> _logger;
    /// <summary>目标端持久化文件目录。</summary>
    private readonly string _targetStoreDirectoryPath;
    /// <summary>目标端持久化文件名前缀。</summary>
    private readonly string _targetStoreFileNamePrefix;
    /// <summary>目标端持久化文件扩展名。</summary>
    private readonly string _targetStoreFileNameExtension;
    /// <summary>运行期存储守护服务。</summary>
    private readonly IRuntimeStorageGuard _runtimeStorageGuard;
    /// <summary>是否启用空闲驱逐。</summary>
    private readonly bool _enableIdleEviction;
    /// <summary>空闲驱逐阈值（Stopwatch Ticks）。</summary>
    private readonly long _idleEvictionThresholdTicks;
    /// <summary>空闲驱逐阈值分钟数（用于日志输出，避免运行时反向计算带来溢出风险）。</summary>
    private readonly double _idleEvictionThresholdMinutes;
    /// <summary>目标端文件归档最大保留数量（0 表示关闭）。</summary>
    private readonly int _targetStoreArchiveMaxCount;

    /// <summary>
    /// 初始化同步幂等合并仓储。
    /// </summary>
    /// <param name="syncJobOptions">同步任务配置。</param>
    /// <param name="runtimeStorageGuard">运行期存储守护服务。</param>
    /// <param name="logger">日志组件。</param>
    public SyncUpsertRepository(
        IOptions<SyncJobOptions> syncJobOptions,
        IRuntimeStorageGuard runtimeStorageGuard,
        ILogger<SyncUpsertRepository> logger)
    {
        _logger = logger;
        _runtimeStorageGuard = runtimeStorageGuard;
        var opts = syncJobOptions.Value;
        var resolvedTargetStoreFilePath = RuntimeStoragePathResolver.ResolveAbsolutePath(
            opts.TargetStoreFilePath,
            Path.Combine("data", "sync-target-store.json"));
        _targetStoreDirectoryPath = ResolveTargetStoreDirectoryPath(resolvedTargetStoreFilePath);
        _targetStoreFileNamePrefix = ResolveTargetStoreFileNamePrefix(resolvedTargetStoreFilePath);
        _targetStoreFileNameExtension = ResolveTargetStoreFileNameExtension(resolvedTargetStoreFilePath);
        _enableIdleEviction = opts.EnableIdleEviction;
        var thresholdMinutes = opts.IdleEvictionThresholdMinutes >= 1 ? opts.IdleEvictionThresholdMinutes : 30;
        _idleEvictionThresholdMinutes = thresholdMinutes;
        _idleEvictionThresholdTicks = (long)(TimeSpan.FromMinutes(thresholdMinutes).TotalSeconds * Stopwatch.Frequency);
        _targetStoreArchiveMaxCount = opts.TargetStoreArchiveMaxCount >= 0 ? opts.TargetStoreArchiveMaxCount : 7;
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
        TouchTableAccessTime(request.TableCode);
        var targetTable = _targetTables.GetOrAdd(request.TableCode, _ => CreateBusinessKeyDictionary());
        await _runtimeStorageGuard.ReportTableMemoryAsync(request.TableCode, targetTable.Count, "目标快照合并前", ct);
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

            var newState = BuildTargetStateRow(rowKey, filteredRow, request.CursorColumn);
            if (!targetTable.TryGetValue(rowKey, out var existedRow))
            {
                targetTable[rowKey] = newState;
                result.InsertCount++;
                changedOperations[rowKey] = SyncChangeOperationType.Insert;
                UpdateLastCursor(result, newState.CursorLocal);
                hasChanges = true;
                continue;
            }

            if (IsStateEqual(existedRow, newState))
            {
                result.SkipCount++;
                UpdateLastCursor(result, newState.CursorLocal);
                continue;
            }

            targetTable[rowKey] = newState;
            result.UpdateCount++;
            changedOperations[rowKey] = SyncChangeOperationType.Update;
            UpdateLastCursor(result, newState.CursorLocal);
            hasChanges = true;
        }

        if (hasChanges)
        {
            await PersistTableAsync(request.TableCode, targetTable, ct);
        }
        await _runtimeStorageGuard.ReportTableMemoryAsync(request.TableCode, targetTable.Count, "目标快照合并后", ct);

        return result;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SyncTargetStateRow>> ListTargetStateRowsAsync(string tableCode, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await EnsureTableLoadedAsync(tableCode, ct);
        TouchTableAccessTime(tableCode);
        if (!_targetTables.TryGetValue(tableCode, out var table))
        {
            return [];
        }

        return table.Values.ToList();
    }

    /// <inheritdoc/>
    public async Task<int> DeleteByBusinessKeysAsync(string tableCode, IReadOnlyList<string> businessKeys, DeletionPolicy deletionPolicy, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await EnsureTableLoadedAsync(tableCode, ct);
        TouchTableAccessTime(tableCode);
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
                var softDeletedRow = new SyncTargetStateRow
                {
                    BusinessKey = row.BusinessKey,
                    RowDigest = row.RowDigest,
                    CursorLocal = row.CursorLocal,
                    IsSoftDeleted = true,
                    SoftDeletedTimeLocal = DateTime.Now,
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
        await _runtimeStorageGuard.ReportTableMemoryAsync(tableCode, table.Count, "目标快照删除后", ct);

        return deletedCount;
    }


    /// <summary>
    /// 判断两行是否一致。
    /// </summary>
    /// <param name="left">旧值。</param>
    /// <param name="right">新值。</param>
    /// <returns>一致返回 <c>true</c>。</returns>
    private static bool IsStateEqual(SyncTargetStateRow left, SyncTargetStateRow right)
    {
        return string.Equals(left.RowDigest, right.RowDigest, StringComparison.Ordinal)
               && Nullable.Equals(left.CursorLocal, right.CursorLocal)
               && left.IsSoftDeleted == right.IsSoftDeleted
               && Nullable.Equals(left.SoftDeletedTimeLocal, right.SoftDeletedTimeLocal);
    }

    /// <summary>
    /// 更新最大成功游标。
    /// </summary>
    /// <param name="result">合并结果。</param>
    /// <param name="row">数据行。</param>
    /// <param name="cursorColumn">游标列。</param>
    private static void UpdateLastCursor(SyncMergeResult result, DateTime? cursorLocal)
    {
        if (!cursorLocal.HasValue)
        {
            return;
        }

        if (!result.LastSuccessCursorLocal.HasValue || cursorLocal.Value > result.LastSuccessCursorLocal.Value)
        {
            result.LastSuccessCursorLocal = cursorLocal.Value;
        }
    }

    /// <summary>
    /// 克隆行数据。
    /// </summary>
    /// <param name="row">原始行。</param>
    /// <returns>克隆结果。</returns>
    private static SyncTargetStateRow CloneRow(SyncTargetStateRow row)
    {
        return row with { };
    }

    /// <summary>
    /// 创建业务键字典（忽略大小写）。
    /// </summary>
    /// <returns>业务键字典。</returns>
    private static ConcurrentDictionary<string, SyncTargetStateRow> CreateBusinessKeyDictionary()
    {
        return new ConcurrentDictionary<string, SyncTargetStateRow>(StringComparer.OrdinalIgnoreCase);
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
    private async Task PersistTableAsync(string tableCode, ConcurrentDictionary<string, SyncTargetStateRow> table, CancellationToken ct)
    {
        var tableStoreFilePath = BuildPerTableStoreFilePath(tableCode);
        await _runtimeStorageGuard.EnsureWriteSpaceAsync(tableStoreFilePath, $"目标快照写入[{tableCode}]", ct);
        var persistedTable = new Dictionary<string, SyncTargetStateRow>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in table)
        {
            persistedTable[pair.Key] = CloneRow(pair.Value);
        }

        await TargetStoreFileLock.WaitAsync(ct);
        try
        {
            await SaveTableRowsWithoutLockAsync(tableCode, persistedTable, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写入同步目标落地文件失败。TableCode={TableCode}, TableStoreFilePath={TableStoreFilePath}", tableCode, tableStoreFilePath);
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
    private async Task<Dictionary<string, SyncTargetStateRow>> LoadTableRowsWithoutLockAsync(string tableCode, CancellationToken ct)
    {
        var tableStoreFilePath = BuildPerTableStoreFilePath(tableCode);
        if (File.Exists(tableStoreFilePath))
        {
            var tableJson = await File.ReadAllTextAsync(tableStoreFilePath, ct);
            if (string.IsNullOrWhiteSpace(tableJson))
            {
                return new Dictionary<string, SyncTargetStateRow>(StringComparer.OrdinalIgnoreCase);
            }

            return DeserializeTableRows(tableJson, tableStoreFilePath, tableCode);
        }

        return new Dictionary<string, SyncTargetStateRow>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 保存指定表持久化数据（调用方需保证已持锁）。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    /// <param name="tableRows">指定表数据。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task SaveTableRowsWithoutLockAsync(string tableCode, Dictionary<string, SyncTargetStateRow> tableRows, CancellationToken ct)
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
                File.Replace(tempFilePath, tableStoreFilePath, backupFilePath, ignoreMetadataErrors: false);
                if (File.Exists(backupFilePath))
                {
                    // 若已开启归档压缩，将旧版本压缩为 .{timestamp}.json.gz 并清理超限的旧归档。
                    if (_targetStoreArchiveMaxCount > 0)
                    {
                        ArchiveBackupFile(tableCode, tableStoreFilePath, backupFilePath);
                    }
                    else
                    {
                        File.Delete(backupFilePath);
                    }
                }
            }
            else
            {
                File.Move(tempFilePath, tableStoreFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "原子写入同步目标落地文件失败。Path={TargetStoreFilePath}, TableCode={TableCode}", tableStoreFilePath, tableCode);
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
    /// 将旧版本备份文件压缩归档，并清理超出保留数量的最旧归档文件。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    /// <param name="activeFilePath">当前活跃文件路径（用于匹配归档前缀）。</param>
    /// <param name="backupFilePath">待归档的 .bak 文件路径。</param>
    private void ArchiveBackupFile(string tableCode, string activeFilePath, string backupFilePath)
    {
        try
        {
            // 含毫秒精度，避免高频写入场景下同秒内文件名冲突。
            // 去掉活跃文件扩展名后拼接时间戳，避免双重扩展名（如 .json.{ts}.json.gz）。
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            var activeFileBasePath = activeFilePath[..^Path.GetExtension(activeFilePath).Length];
            var archiveFilePath = $"{activeFileBasePath}.{timestamp}.json.gz";
            // 使用 GZip 压缩 .bak 文件到归档。
            using (var inputStream = File.OpenRead(backupFilePath))
            using (var outputStream = File.Create(archiveFilePath))
            using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal))
            {
                inputStream.CopyTo(gzipStream);
            }

            File.Delete(backupFilePath);
            TrimOldArchives(tableCode, activeFilePath);
        }
        catch (Exception ex)
        {
            // 归档失败不应阻断正常写入流程，仅记录警告。
            _logger.LogWarning(ex, "压缩归档旧版本目标快照文件失败，将直接删除备份文件。TableCode={TableCode}, BackupPath={BackupPath}", tableCode, backupFilePath);
            try
            {
                if (File.Exists(backupFilePath))
                {
                    File.Delete(backupFilePath);
                }
            }
            catch (Exception deleteEx)
            {
                _logger.LogWarning(deleteEx, "删除备份文件失败。BackupPath={BackupPath}", backupFilePath);
            }
        }
    }

    /// <summary>
    /// 清理超出保留数量的最旧压缩归档文件。
    /// 按文件最后写入时间升序排序，确保最旧的文件最先被清理，不依赖文件名字典序。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    /// <param name="activeFilePath">当前活跃文件路径（用于匹配归档前缀）。</param>
    private void TrimOldArchives(string tableCode, string activeFilePath)
    {
        try
        {
            // 归档文件名模式：去掉活跃文件扩展名后，拼接 .{timestamp}.json.gz，故 glob 模式也需先去掉扩展名。
            var activeFileNameNoExt = Path.GetFileNameWithoutExtension(activeFilePath);
            var archivePattern = $"{activeFileNameNoExt}.*.json.gz";
            // 按文件最后写入时间升序排序，确保最旧文件最先被清理。
            var archives = Directory
                .GetFiles(_targetStoreDirectoryPath, archivePattern)
                .OrderBy(f => new FileInfo(f).LastWriteTime)
                .ToList();
            var excessCount = archives.Count - _targetStoreArchiveMaxCount;
            if (excessCount <= 0)
            {
                return;
            }

            for (var i = 0; i < excessCount; i++)
            {
                try
                {
                    File.Delete(archives[i]);
                    _logger.LogInformation(
                        "清理超限目标快照压缩归档。TableCode={TableCode}, Path={ArchivePath}",
                        tableCode,
                        archives[i]);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "清理超限目标快照压缩归档失败。TableCode={TableCode}, Path={ArchivePath}", tableCode, archives[i]);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "枚举目标快照压缩归档文件失败。TableCode={TableCode}", tableCode);
        }
    }

    /// <summary>
    /// 构建按表落地文件路径。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    /// <returns>按表落地文件绝对路径。</returns>
    private string BuildPerTableStoreFilePath(string tableCode)
    {
        var safeTableCode = ReplaceInvalidFileNameChars(tableCode);
        var fileName = $"{_targetStoreFileNamePrefix}.{safeTableCode}{_targetStoreFileNameExtension}";
        return Path.Combine(_targetStoreDirectoryPath, fileName);
    }

    /// <summary>
    /// 将非法文件名字符替换为下划线。
    /// </summary>
    /// <param name="value">原始字符串。</param>
    /// <returns>替换结果。</returns>
    private static string ReplaceInvalidFileNameChars(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars().ToHashSet();
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (invalidChars.Contains(chars[i]))
            {
                chars[i] = '_';
            }
        }

        return new string(chars);
    }

    /// <summary>
    /// 反序列化按表持久化数据并恢复字段类型。
    /// </summary>
    /// <param name="json">JSON 文本。</param>
    /// <param name="path">文件路径。</param>
    /// <param name="tableCode">表编码。</param>
    /// <returns>按表数据。</returns>
    private Dictionary<string, SyncTargetStateRow> DeserializeTableRows(string json, string path, string tableCode)
    {
        try
        {
            var deserialized = JsonSerializer.Deserialize<Dictionary<string, SyncTargetStateRow>>(json)
                ?? new Dictionary<string, SyncTargetStateRow>(StringComparer.OrdinalIgnoreCase);
            return NormalizeTableRows(deserialized);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取同步目标落地文件失败。Path={TargetStoreFilePath}, TableCode={TableCode}", path, tableCode);
            throw new InvalidOperationException(
                $"读取同步目标落地文件失败（Path={path}, TableCode={tableCode}）。请检查文件内容是否为有效 JSON，必要时备份后清理该运行期文件再重试。",
                ex);
        }
    }

    /// <summary>
    /// 归一化轻量幂等状态，避免 JsonElement 影响后续比较与游标计算。
    /// </summary>
    /// <param name="rows">按表状态。</param>
    /// <returns>归一化结果。</returns>
    private static Dictionary<string, SyncTargetStateRow> NormalizeTableRows(Dictionary<string, SyncTargetStateRow> rows)
    {
        var normalized = new Dictionary<string, SyncTargetStateRow>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in rows)
        {
            normalized[pair.Key] = NormalizeTargetStateRow(pair.Key, pair.Value);
        }

        return normalized;
    }

    /// <summary>
    /// 归一化轻量状态行。
    /// </summary>
    /// <param name="businessKey">业务键。</param>
    /// <param name="row">原始状态行。</param>
    /// <returns>归一化状态行。</returns>
    private static SyncTargetStateRow NormalizeTargetStateRow(string businessKey, SyncTargetStateRow row)
    {
        return new SyncTargetStateRow
        {
            BusinessKey = string.IsNullOrWhiteSpace(row.BusinessKey) ? businessKey : row.BusinessKey,
            RowDigest = row.RowDigest ?? string.Empty,
            CursorLocal = NormalizeOptionalLocalDateTime(row.CursorLocal, $"状态文件[{businessKey}]游标"),
            IsSoftDeleted = row.IsSoftDeleted,
            SoftDeletedTimeLocal = NormalizeOptionalLocalDateTime(row.SoftDeletedTimeLocal, $"状态文件[{businessKey}]软删除时间"),
        };
    }


    /// <summary>
    /// 构建目标端轻量幂等状态行。
    /// </summary>
    /// <param name="businessKey">业务键。</param>
    /// <param name="row">原始业务行。</param>
    /// <param name="cursorColumn">游标列。</param>
    /// <returns>轻量状态行。</returns>
    private static SyncTargetStateRow BuildTargetStateRow(
        string businessKey,
        IReadOnlyDictionary<string, object?> row,
        string cursorColumn)
    {
        return new SyncTargetStateRow
        {
            BusinessKey = businessKey,
            RowDigest = ComputeRowDigestHash(row),
            CursorLocal = TryGetCursorLocal(row, cursorColumn),
            IsSoftDeleted = IsSoftDeleted(row),
            SoftDeletedTimeLocal = TryGetSoftDeletedTimeLocal(row),
        };
    }

    /// <summary>
    /// 计算行摘要哈希（SHA256 十六进制文本）。
    /// </summary>
    /// <param name="row">业务行。</param>
    /// <returns>摘要文本。</returns>
    private static string ComputeRowDigestHash(IReadOnlyDictionary<string, object?> row)
    {
        var sortedKeys = row.Keys.ToArray();
        Array.Sort(sortedKeys, StringComparer.OrdinalIgnoreCase);
        using var incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var key in sortedKeys)
        {
            AppendLengthPrefixedUtf8(incrementalHash, key);
            var normalizedValue = NormalizeDigestValue(row[key]);
            AppendLengthPrefixedUtf8(incrementalHash, ConvertDigestValueToStableText(normalizedValue));
        }

        var hashBytes = incrementalHash.GetHashAndReset();
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// 将字符串按“长度前缀 + UTF-8 内容”追加到增量哈希，避免分隔符冲突。
    /// </summary>
    /// <param name="incrementalHash">增量哈希实例。</param>
    /// <param name="value">待追加字符串。</param>
    private static void AppendLengthPrefixedUtf8(IncrementalHash incrementalHash, string value)
    {
        var byteCount = System.Text.Encoding.UTF8.GetByteCount(value);
        Span<byte> lengthPrefix = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(lengthPrefix, byteCount);
        incrementalHash.AppendData(lengthPrefix);
        var buffer = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            var written = System.Text.Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, 0);
            incrementalHash.AppendData(buffer.AsSpan(0, written));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// 将归一化值转换为稳定文本，避免热路径中频繁创建 JSON 数组对象。
    /// </summary>
    /// <param name="value">归一化值。</param>
    /// <returns>稳定文本。</returns>
    private static string ConvertDigestValueToStableText(object? value)
    {
        if (value is null)
        {
            return "null";
        }

        if (value is string text)
        {
            return text;
        }

        if (value is bool booleanValue)
        {
            return booleanValue ? "true" : "false";
        }

        if (value is IFormattable formattable)
        {
            return formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        return JsonSerializer.Serialize(value, DigestSerializerOptions);
    }

    /// <summary>
    /// 归一化摘要计算值。
    /// </summary>
    /// <param name="value">原始值。</param>
    /// <returns>归一化值。</returns>
    private static object? NormalizeDigestValue(object? value)
    {
        if (value is DateTime dateTime)
        {
            return EnsureLocalDateTime(dateTime, dateTime.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture))
                .ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture);
        }

        return value;
    }

    /// <summary>
    /// 尝试提取游标本地时间。
    /// </summary>
    /// <param name="row">业务行。</param>
    /// <param name="cursorColumn">游标列名。</param>
    /// <returns>游标本地时间。</returns>
    private static DateTime? TryGetCursorLocal(IReadOnlyDictionary<string, object?> row, string cursorColumn)
    {
        if (string.IsNullOrWhiteSpace(cursorColumn))
        {
            return null;
        }

        if (!row.TryGetValue(cursorColumn, out var cursorValue) || cursorValue is null)
        {
            return null;
        }

        if (cursorValue is DateTime cursorDateTime)
        {
            return EnsureLocalDateTime(cursorDateTime, cursorDateTime.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture));
        }

        if (cursorValue is string cursorText)
        {
            if (ContainsOffsetOrZulu(cursorText))
            {
                throw new InvalidOperationException($"不支持包含 Z 或 offset 的时间文本：{cursorText}");
            }

            if (DateTime.TryParse(
                    cursorText,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces,
                    out var parsedLocalDateTime))
            {
                return EnsureLocalDateTime(parsedLocalDateTime, cursorText);
            }
        }

        return null;
    }

    /// <summary>
    /// 判断业务行是否为软删除。
    /// </summary>
    /// <param name="row">业务行。</param>
    /// <returns>软删除返回 <c>true</c>。</returns>
    private static bool IsSoftDeleted(IReadOnlyDictionary<string, object?> row)
    {
        return row.TryGetValue(SyncColumnFilter.SoftDeleteFlagColumn, out var flagValue)
               && flagValue is bool flag
               && flag;
    }

    /// <summary>
    /// 尝试提取软删除本地时间。
    /// </summary>
    /// <param name="row">业务行。</param>
    /// <returns>软删除时间。</returns>
    private static DateTime? TryGetSoftDeletedTimeLocal(IReadOnlyDictionary<string, object?> row)
    {
        if (!row.TryGetValue(SyncColumnFilter.SoftDeleteTimeColumn, out var timeValue) || timeValue is null)
        {
            return null;
        }

        if (timeValue is DateTime dateTime)
        {
            return EnsureLocalDateTime(dateTime, dateTime.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture));
        }

        if (timeValue is string textValue)
        {
            if (ContainsOffsetOrZulu(textValue))
            {
                throw new InvalidOperationException($"不支持包含 Z 或 offset 的时间文本：{textValue}");
            }

            if (DateTime.TryParse(
                    textValue,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces,
                    out var parsedLocalDateTime))
            {
                return EnsureLocalDateTime(parsedLocalDateTime, textValue);
            }
        }

        return null;
    }

    /// <summary>
    /// 归一化可空本地时间。
    /// </summary>
    /// <param name="value">时间值。</param>
    /// <param name="name">字段名称。</param>
    /// <returns>归一化时间。</returns>
    private static DateTime? NormalizeOptionalLocalDateTime(DateTime? value, string name)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return EnsureLocalDateTime(value.Value, name);
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
        if (value.EndsWith("Z", StringComparison.Ordinal))
        {
            return true;
        }

        // 同时兼容 ISO 8601（T 分隔）和常见本地格式（空格分隔）。
        var separatorIndex = value.IndexOf('T', StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            separatorIndex = value.IndexOf(' ', StringComparison.Ordinal);
        }

        if (separatorIndex < 0 || separatorIndex >= value.Length - 1)
        {
            return false;
        }

        var timePart = value[(separatorIndex + 1)..];
        for (var i = 0; i < timePart.Length; i++)
        {
            var current = timePart[i];
            if (current != '+' && current != '-')
            {
                continue;
            }

            var remainLength = timePart.Length - i;
            if (remainLength < 6)
            {
                continue;
            }

            var hasValidOffsetPattern = char.IsDigit(timePart[i + 1])
                                        && char.IsDigit(timePart[i + 2])
                                        && timePart[i + 3] == ':'
                                        && char.IsDigit(timePart[i + 4])
                                        && char.IsDigit(timePart[i + 5]);
            if (!hasValidOffsetPattern)
            {
                continue;
            }

            if (remainLength == 6 || !char.IsDigit(timePart[i + 6]))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 更新指定表的最后访问时间戳。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    private void TouchTableAccessTime(string tableCode)
    {
        _tableLastAccessTimestamps[tableCode] = Stopwatch.GetTimestamp();
    }

    /// <inheritdoc/>
    public Task<int> EvictIdleTablesAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!_enableIdleEviction)
        {
            return Task.FromResult(0);
        }

        var now = Stopwatch.GetTimestamp();
        var evictedCount = 0;
        foreach (var tableCode in _targetTables.Keys)
        {
            ct.ThrowIfCancellationRequested();
            // 若表从未记录访问时间，说明是新加载但未被实际访问的边界情况，跳过驱逐。
            if (!_tableLastAccessTimestamps.TryGetValue(tableCode, out var lastAccessTick))
            {
                continue;
            }

            if (now - lastAccessTick < _idleEvictionThresholdTicks)
            {
                continue;
            }

            // 将表从内存中卸载；持久化文件已在写入时落盘，卸载后下次访问会按需重新加载。
            if (_targetTables.TryRemove(tableCode, out _))
            {
                _tableLastAccessTimestamps.TryRemove(tableCode, out _);
                evictedCount++;
                _logger.LogInformation(
                    "空闲表内存已驱逐。TableCode={TableCode}, IdleEvictionThresholdMinutes={ThresholdMinutes}",
                    tableCode,
                    _idleEvictionThresholdMinutes);
            }
        }

        if (evictedCount > 0)
        {
            _logger.LogInformation("空闲表内存驱逐完成。EvictedCount={EvictedCount}", evictedCount);
        }

        return Task.FromResult(evictedCount);
    }
}
