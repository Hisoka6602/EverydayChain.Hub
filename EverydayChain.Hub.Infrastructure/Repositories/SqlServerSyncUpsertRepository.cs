using System.Text;
using System.Buffers;
using System.Globalization;
using System.Data;
using System.Buffers.Binary;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.SharedKernel.Utilities;
using EverydayChain.Hub.Infrastructure.Services;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using Newtonsoft.Json;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SqlServerSyncUpsertRepository(
    IOptions<SyncJobOptions> syncJobOptions,
    IOptions<ShardingOptions> shardingOptions,
    IShardSuffixResolver shardSuffixResolver,
    IShardTableProvisioner shardTableProvisioner,
    IDangerZoneExecutor dangerZoneExecutor,
    ILogger<SqlServerSyncUpsertRepository> logger) : ISyncUpsertRepository {

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string SyncTargetStateTablePrefix = "sync_target_state";

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string SyncTargetStateSchema = "dbo";

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const int StateMonthTokenLength = 6;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const int TempTableCleanupTimeoutSeconds = 5;

    private static readonly JsonSerializerSettings DigestSerializerSettings = new() {
        Formatting = Formatting.None,
    };

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly SyncJobOptions _syncJobOptions = syncJobOptions.Value;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly ShardingOptions _shardingOptions = shardingOptions.Value;

    private readonly IReadOnlyDictionary<string, SyncTableOptions> _tableOptionsMap = BuildTableOptionsMap(syncJobOptions.Value);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public Task<SyncMergeResult> MergeFromStagingAsync(SyncMergeRequest request, CancellationToken ct) {
        // 步骤：按既定流程执行当前方法逻辑。
        if (request.UniqueKeys.Count == 0) {
            throw new InvalidOperationException($"同步表 {request.TableCode} 未配置 UniqueKeys，无法执行幂等合并。");
        }

        return dangerZoneExecutor.ExecuteAsync(
            $"sqlserver-upsert-merge-{request.TableCode}",
            token => MergeCoreAsync(request, token),
            ct);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public Task<IReadOnlyList<SyncTargetStateRow>> ListTargetStateRowsAsync(string tableCode, CancellationToken ct) {
        // 步骤：按既定流程执行当前方法逻辑。
        return dangerZoneExecutor.ExecuteAsync(
            $"sqlserver-upsert-list-state-{tableCode}",
            token => ListTargetStateRowsCoreAsync(tableCode, token),
            ct);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public Task<int> DeleteByBusinessKeysAsync(string tableCode, IReadOnlyList<string> businessKeys, DeletionPolicy deletionPolicy, CancellationToken ct) {
        // 步骤：按既定流程执行当前方法逻辑。
        if (businessKeys.Count == 0 || deletionPolicy == DeletionPolicy.Disabled) {
            return Task.FromResult(0);
        }

        return dangerZoneExecutor.ExecuteAsync(
            $"sqlserver-upsert-delete-{tableCode}",
            token => DeleteByBusinessKeysCoreAsync(tableCode, businessKeys, deletionPolicy, token),
            ct);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    protected virtual async Task<SyncMergeResult> MergeCoreAsync(SyncMergeRequest request, CancellationToken ct) {
        // 步骤：按既定流程执行当前方法逻辑。
        if (request.UniqueKeys.Count == 0) {
            throw new InvalidOperationException($"同步表 {request.TableCode} 未配置 UniqueKeys，无法执行幂等合并。");
        }

        var changedOperations = new Dictionary<string, SyncChangeOperationType>(StringComparer.OrdinalIgnoreCase);
        var result = new SyncMergeResult {
            ChangedOperations = changedOperations,
        };
        if (request.Rows.Count == 0) {
            return result;
        }

        var tableOptions = ResolveTableOptions(request.TableCode);
        var targetLogicalTable = ResolveTargetLogicalTable(request.TableCode, tableOptions);
        var entries = BuildMergeEntries(request, targetLogicalTable);
        if (entries.Count == 0) {
            return result;
        }
        var requiredSuffixes = entries
            .Select(x => x.ShardSuffix)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (requiredSuffixes.Length > 0) {
            await shardTableProvisioner.EnsureShardTablesAsync(requiredSuffixes, ct);
        }

        await using var connection = new SqlConnection(_shardingOptions.ConnectionString);
        await connection.OpenAsync(ct);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(ct);
        try {
            var stateMonthToken = ResolveStateMonthToken(DateTime.Now);
            await EnsureSyncTargetStateTableExistsAsync(request.TableCode, stateMonthToken, connection, transaction, ct);
            var distinctBusinessKeys = GetDistinctBusinessKeys(entries);
            var states = await LoadStateMapAsync(connection, transaction, request.TableCode, distinctBusinessKeys, ct);
            var initialStates = new Dictionary<string, PersistedState>(states, StringComparer.OrdinalIgnoreCase);
            var latestChangedEntries = new Dictionary<string, MergeEntry>(StringComparer.OrdinalIgnoreCase);
            var changedBusinessKeysInOrder = new List<string>(entries.Count);
            foreach (var entry in entries) {
                ct.ThrowIfCancellationRequested();
                UpdateLastCursor(result, entry.State.CursorLocal);

                if (!states.TryGetValue(entry.BusinessKey, out var existingState)) {
                    TrackLatestChangedEntry(latestChangedEntries, changedBusinessKeysInOrder, entry);
                    states[entry.BusinessKey] = new PersistedState(
                        entry.BusinessKey,
                        entry.State.RowDigest,
                        entry.State.CursorLocal,
                        entry.State.IsSoftDeleted,
                        entry.State.SoftDeletedTimeLocal,
                        entry.ShardSuffix,
                        entry.TargetLogicalTable);
                    result.InsertCount++;
                    changedOperations[entry.BusinessKey] = SyncChangeOperationType.Insert;
                    continue;
                }

                if (IsPersistedStateEqual(existingState, entry.State, entry.ShardSuffix)) {
                    result.SkipCount++;
                    continue;
                }

                TrackLatestChangedEntry(latestChangedEntries, changedBusinessKeysInOrder, entry);
                states[entry.BusinessKey] = new PersistedState(
                    entry.BusinessKey,
                    entry.State.RowDigest,
                    entry.State.CursorLocal,
                    entry.State.IsSoftDeleted,
                    entry.State.SoftDeletedTimeLocal,
                    entry.ShardSuffix,
                    entry.TargetLogicalTable);
                result.UpdateCount++;
                changedOperations[entry.BusinessKey] = SyncChangeOperationType.Update;
            }

            var changedEntries = BuildOrderedChangedEntries(changedBusinessKeysInOrder, latestChangedEntries);
            var shardSwitchDeletes = BuildShardSwitchDeletes(changedEntries, initialStates);
            var batchMergeSize = GetBatchMergeSize();
            if (_syncJobOptions.EnableSetBasedMerge) {
                await ExecuteShardDeleteBatchesAsync(connection, transaction, request.UniqueKeys, shardSwitchDeletes, batchMergeSize, ct);
                await ExecuteTargetMergeBatchesAsync(connection, transaction, request.UniqueKeys, changedEntries, batchMergeSize, ct);
                await UpsertStatesBatchAsync(connection, transaction, request.TableCode, stateMonthToken, changedEntries, batchMergeSize, ct);
            }
            else {
                var uniqueKeySet = new HashSet<string>(request.UniqueKeys, StringComparer.OrdinalIgnoreCase);
                foreach (var shardDelete in shardSwitchDeletes) {
                    await DeleteTargetRowByBusinessKeyAsync(
                        connection,
                        transaction,
                        shardDelete.TargetLogicalTable,
                        shardDelete.ShardSuffix,
                        request.UniqueKeys,
                        shardDelete.BusinessKey,
                        ct);
                }

                foreach (var entry in changedEntries) {
                    await UpsertTargetRowAsync(connection, transaction, entry.TargetLogicalTable, entry.ShardSuffix, entry.Row, request.UniqueKeys, uniqueKeySet, ct);
                    await UpsertStateAsync(connection, transaction, request.TableCode, stateMonthToken, entry, ct);
                }
            }

            await transaction.CommitAsync(ct);
            return result;
        }
        catch (Exception ex) {
            logger.LogError(ex, "SQL Server 幂等合并失败。TableCode={TableCode}", request.TableCode);
            try {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            catch (Exception rollbackException) {
                logger.LogError(rollbackException, "SQL Server 幂等合并事务回滚失败。TableCode={TableCode}", request.TableCode);
            }

            throw;
        }
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    protected virtual async Task<IReadOnlyList<SyncTargetStateRow>> ListTargetStateRowsCoreAsync(string tableCode, CancellationToken ct) {
        // 步骤：按既定流程执行当前方法逻辑。
        var stateMap = new Dictionary<string, (SyncTargetStateRow State, DateTime UpdatedTimeLocal)>(StringComparer.OrdinalIgnoreCase);
        await using var connection = new SqlConnection(_shardingOptions.ConnectionString);
        await connection.OpenAsync(ct);
        var stateMonthToken = ResolveStateMonthToken(DateTime.Now);
        /// <summary>
        /// 执行当前方法。
        /// </summary>
        await EnsureSyncTargetStateTableExistsAsync(tableCode, stateMonthToken, connection, transaction: null, ct);
        var stateTables = await ListSyncStateTableFullNamesAsync(tableCode, connection, transaction: null, ct);
        foreach (var stateTable in stateTables) {
            await using var command = connection.CreateCommand();
            command.CommandText = $@"
SELECT [BusinessKey], [RowDigest], [CursorLocal], [IsSoftDeleted], [SoftDeletedTimeLocal], [UpdatedTimeLocal]
FROM {stateTable}
WHERE [TableCode]=@tableCode;";
            command.Parameters.AddWithValue("@tableCode", tableCode);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) {
                var businessKey = reader.GetString(0);
                var current = new SyncTargetStateRow {
                    BusinessKey = businessKey,
                    RowDigest = reader.GetString(1),
                    CursorLocal = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                    IsSoftDeleted = reader.GetBoolean(3),
                    SoftDeletedTimeLocal = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                };
                var updatedTimeLocal = reader.GetDateTime(5);
                if (stateMap.TryGetValue(businessKey, out var existed) && existed.UpdatedTimeLocal >= updatedTimeLocal) {
                    continue;
                }

                stateMap[businessKey] = (current, updatedTimeLocal);
            }
        }

        return stateMap
            .Select(pair => pair.Value.State)
            .ToArray();
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    protected virtual async Task<int> DeleteByBusinessKeysCoreAsync(string tableCode, IReadOnlyList<string> businessKeys, DeletionPolicy deletionPolicy, CancellationToken ct) {
        // 步骤：按既定流程执行当前方法逻辑。
        var tableOptions = ResolveTableOptions(tableCode);
        if (tableOptions.UniqueKeys.Count == 0) {
            throw new InvalidOperationException($"同步表 {tableCode} 未配置 UniqueKeys，无法执行删除。");
        }

        await using var connection = new SqlConnection(_shardingOptions.ConnectionString);
        await connection.OpenAsync(ct);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(ct);
        try {
            var distinctBusinessKeys = GetDistinctBusinessKeys(businessKeys);
            var stateMap = await LoadStateMapAsync(connection, transaction, tableCode, distinctBusinessKeys, ct);
            var deletedCount = 0;
            foreach (var businessKey in distinctBusinessKeys) {
                ct.ThrowIfCancellationRequested();
                if (!stateMap.TryGetValue(businessKey, out var state)) {
                    continue;
                }

                if (deletionPolicy == DeletionPolicy.SoftDelete) {
                    await MarkSoftDeletedStateAsync(connection, transaction, tableCode, businessKey, ct);
                    deletedCount++;
                    continue;
                }

                if (deletionPolicy == DeletionPolicy.HardDelete) {
                    await DeleteTargetRowByBusinessKeyAsync(
                        connection,
                        transaction,
                        state.TargetLogicalTable,
                        state.ShardSuffix,
                        tableOptions.UniqueKeys,
                        businessKey,
                        ct);
                    await DeleteStateAsync(connection, transaction, tableCode, businessKey, ct);
                    deletedCount++;
                }
            }

            await transaction.CommitAsync(ct);
            return deletedCount;
        }
        catch (Exception ex) {
            logger.LogError(ex, "SQL Server 目标端删除失败。TableCode={TableCode}, DeletionPolicy={DeletionPolicy}", tableCode, deletionPolicy);
            try {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            catch (Exception rollbackException) {
                logger.LogError(rollbackException, "SQL Server 目标端删除事务回滚失败。TableCode={TableCode}, DeletionPolicy={DeletionPolicy}", tableCode, deletionPolicy);
            }

            throw;
        }
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private List<MergeEntry> BuildMergeEntries(SyncMergeRequest request, string targetLogicalTable) {
        // 步骤：按既定流程执行当前方法逻辑。
        var entries = new List<MergeEntry>(request.Rows.Count);
        foreach (var row in request.Rows) {
            var filteredRow = SyncColumnFilter.FilterExcludedColumns(row, request.NormalizedExcludedColumns);
            var businessKey = SyncBusinessKeyBuilder.Build(filteredRow, request.UniqueKeys);
            if (string.IsNullOrWhiteSpace(businessKey)) {
                continue;
            }

            var state = BuildTargetStateRow(businessKey, filteredRow, request.CursorColumn);
            var shardSuffix = ResolveShardSuffix(state.CursorLocal);
            entries.Add(new MergeEntry(
                businessKey,
                targetLogicalTable,
                shardSuffix,
                new Dictionary<string, object?>(filteredRow, StringComparer.OrdinalIgnoreCase),
                state));
        }

        return entries;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private async Task<Dictionary<string, PersistedState>> LoadStateMapAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string tableCode,
        IReadOnlyList<string> businessKeys,
        CancellationToken ct) {
            // 步骤：按既定流程执行当前方法逻辑。
        var stateMap = new Dictionary<string, PersistedState>(StringComparer.OrdinalIgnoreCase);
        if (businessKeys.Count == 0) {
            return stateMap;
        }

        var latestStateMap = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        var stateTables = await ListSyncStateTableFullNamesAsync(tableCode, connection, transaction, ct);
        /// <summary>
        /// 存储当前字段值。
        /// </summary>
        const int chunkSize = 900;
        foreach (var stateTable in stateTables) {
            for (var index = 0; index < businessKeys.Count; index += chunkSize) {
                ct.ThrowIfCancellationRequested();
                var chunkLength = Math.Min(chunkSize, businessKeys.Count - index);
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                var parameterNames = new List<string>(chunkLength);
                for (var i = 0; i < chunkLength; i++) {
                    var businessKey = businessKeys[index + i];
                    var parameterName = $"@businessKey{i}";
                    parameterNames.Add(parameterName);
                    command.Parameters.AddWithValue(parameterName, businessKey);
                }

                command.Parameters.AddWithValue("@tableCode", tableCode);
                command.CommandText = $@"
SELECT [BusinessKey], [RowDigest], [CursorLocal], [IsSoftDeleted], [SoftDeletedTimeLocal], [ShardSuffix], [TargetLogicalTable], [UpdatedTimeLocal]
FROM {stateTable}
WHERE [TableCode]=@tableCode
  AND [BusinessKey] IN ({string.Join(", ", parameterNames)});";
                await using var reader = await command.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct)) {
                    var persisted = new PersistedState(
                        reader.GetString(0),
                        reader.GetString(1),
                        reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                        reader.GetBoolean(3),
                        reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                        reader.GetString(5),
                        reader.GetString(6));
                    var updatedTimeLocal = reader.GetDateTime(7);
                    if (latestStateMap.TryGetValue(persisted.BusinessKey, out var latestUpdatedTime)
                        && latestUpdatedTime >= updatedTimeLocal) {
                        continue;
                    }

                    latestStateMap[persisted.BusinessKey] = updatedTimeLocal;
                    stateMap[persisted.BusinessKey] = persisted;
                }
            }
        }

        return stateMap;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private async Task EnsureSyncTargetStateTableExistsAsync(string tableCode, string stateMonthToken, SqlConnection connection, SqlTransaction? transaction, CancellationToken ct) {
        // 步骤：按既定流程执行当前方法逻辑。
        var fullName = GetSyncStateTableFullName(tableCode, stateMonthToken);
        var tableNameRaw = $"{SyncTargetStateTablePrefix}_{tableCode}_{stateMonthToken}";
        var pkName = EscapeIdentifier(BuildStateTableObjectName("PK", tableNameRaw));
        var indexName = EscapeIdentifier(BuildStateTableObjectName("IX", tableNameRaw, "TableCode_CursorLocal"));
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $@"
IF OBJECT_ID(N'{fullName}', N'U') IS NULL
BEGIN
    CREATE TABLE {fullName} (
        [TableCode] NVARCHAR(128) NOT NULL,
        [BusinessKey] NVARCHAR(512) NOT NULL,
        [RowDigest] NVARCHAR(128) NOT NULL,
        [CursorLocal] DATETIME2 NULL,
        [IsSoftDeleted] BIT NOT NULL,
        [SoftDeletedTimeLocal] DATETIME2 NULL,
        [ShardSuffix] NVARCHAR(32) NOT NULL,
        [TargetLogicalTable] NVARCHAR(128) NOT NULL,
        [UpdatedTimeLocal] DATETIME2 NOT NULL,
        CONSTRAINT [{pkName}] PRIMARY KEY ([TableCode], [BusinessKey])
    );

    CREATE INDEX [{indexName}]
    ON {fullName} ([TableCode], [CursorLocal]);
END;";
        await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private int GetBatchMergeSize() {
        // 步骤：按既定流程执行当前方法逻辑。
        return Math.Clamp(_syncJobOptions.BatchMergeSize, 1, 5000);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private async Task ExecuteTargetMergeBatchesAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        IReadOnlyList<string> uniqueKeys,
        IReadOnlyList<MergeEntry> entries,
        int batchMergeSize,
        CancellationToken ct) {
            // 步骤：按既定流程执行当前方法逻辑。
        if (entries.Count == 0) {
            return;
        }

        var groupedEntries = entries
            .GroupBy(entry => new TargetMergeGroupKey(
                entry.TargetLogicalTable,
                entry.ShardSuffix,
                BuildColumnSignature(entry.Row.Keys)))
            .ToArray();
        foreach (var group in groupedEntries) {
            var groupedList = group.ToList();
            for (var index = 0; index < groupedList.Count; index += batchMergeSize) {
                ct.ThrowIfCancellationRequested();
                var currentBatchLength = Math.Min(batchMergeSize, groupedList.Count - index);
                var currentBatch = CreateBatch(groupedList, index, currentBatchLength);
                await ExecuteTargetMergeBatchAsync(connection, transaction, uniqueKeys, currentBatch, ct);
            }
        }
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private async Task ExecuteTargetMergeBatchAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        IReadOnlyList<string> uniqueKeys,
        IReadOnlyList<MergeEntry> entries,
        CancellationToken ct) {
            // 步骤：按既定流程执行当前方法逻辑。
        if (entries.Count == 0) {
            return;
        }

        var sampleEntry = entries[0];
        var fullTableName = BuildTargetTableFullName(sampleEntry.TargetLogicalTable, sampleEntry.ShardSuffix);
        var orderedColumns = sampleEntry.Row.Keys
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(column => column, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        EnsureIdentifiersSafe(orderedColumns);
        EnsureUniqueKeysPresent(uniqueKeys, sampleEntry.Row);
        var uniqueKeySet = new HashSet<string>(uniqueKeys, StringComparer.OrdinalIgnoreCase);

        var tempTableName = $"#sync_merge_{Guid.NewGuid():N}";
        await using (var createCommand = connection.CreateCommand()) {
            createCommand.Transaction = transaction;
            createCommand.CommandText = $@"
SELECT TOP (0) {string.Join(", ", orderedColumns.Select(column => $"[{EscapeIdentifier(column)}]"))}
INTO {tempTableName}
FROM {fullTableName};";
            await createCommand.ExecuteNonQueryAsync(ct);
        }

        try {
            var dataTable = new DataTable();
            foreach (var column in orderedColumns) {
                dataTable.Columns.Add(column, ResolveEntryColumnType(entries, column));
            }

            foreach (var entry in entries) {
                var row = dataTable.NewRow();
                foreach (var column in orderedColumns) {
                    row[column] = entry.Row.TryGetValue(column, out var value)
                        ? ConvertToDbValue(value)
                        : DBNull.Value;
                }

                dataTable.Rows.Add(row);
            }

            using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)) {
                bulkCopy.DestinationTableName = tempTableName;
                foreach (var column in orderedColumns) {
                    bulkCopy.ColumnMappings.Add(column, column);
                }

                await bulkCopy.WriteToServerAsync(dataTable, ct);
            }

            var uniquePredicates = uniqueKeys.Select(key => $"target.[{EscapeIdentifier(key)}] = source.[{EscapeIdentifier(key)}]").ToArray();
            var updateColumns = orderedColumns
                .Where(column => !uniqueKeySet.Contains(column))
                .Select(column => $"target.[{EscapeIdentifier(column)}] = source.[{EscapeIdentifier(column)}]")
                .ToArray();
            var insertColumns = string.Join(", ", orderedColumns.Select(column => $"[{EscapeIdentifier(column)}]"));
            var insertValues = string.Join(", ", orderedColumns.Select(column => $"source.[{EscapeIdentifier(column)}]"));
            var matchedClause = updateColumns.Length == 0
                ? string.Empty
                : $"{Environment.NewLine}WHEN MATCHED THEN{Environment.NewLine}    UPDATE SET {string.Join(", ", updateColumns)}";
            await using var mergeCommand = connection.CreateCommand();
            mergeCommand.Transaction = transaction;
            mergeCommand.CommandText = $@"
MERGE {fullTableName} AS target
USING {tempTableName} AS source
ON {string.Join(" AND ", uniquePredicates)}{matchedClause}
WHEN NOT MATCHED THEN
    INSERT ({insertColumns}) VALUES ({insertValues});";
            await mergeCommand.ExecuteNonQueryAsync(ct);
        }
        finally {
            await DropTempTableAsync(connection, transaction, tempTableName);
        }
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private async Task ExecuteShardDeleteBatchesAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        IReadOnlyList<string> uniqueKeys,
        IReadOnlyList<ShardDeleteEntry> entries,
        int batchMergeSize,
        CancellationToken ct) {
            // 步骤：按既定流程执行当前方法逻辑。
        if (entries.Count == 0) {
            return;
        }

        var groupedEntries = entries
            .GroupBy(entry => new TargetShardGroupKey(entry.TargetLogicalTable, entry.ShardSuffix))
            .ToArray();
        foreach (var group in groupedEntries) {
            var deduplicated = new List<ShardDeleteEntry>(group.Count());
            var seenBusinessKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in group) {
                if (seenBusinessKeys.Add(entry.BusinessKey)) {
                    deduplicated.Add(entry);
                }
            }

            for (var index = 0; index < deduplicated.Count; index += batchMergeSize) {
                ct.ThrowIfCancellationRequested();
                var currentBatchLength = Math.Min(batchMergeSize, deduplicated.Count - index);
                var currentBatch = CreateBatch(deduplicated, index, currentBatchLength);
                await ExecuteShardDeleteBatchAsync(connection, transaction, uniqueKeys, currentBatch, ct);
            }
        }
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private async Task ExecuteShardDeleteBatchAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        IReadOnlyList<string> uniqueKeys,
        IReadOnlyList<ShardDeleteEntry> entries,
        CancellationToken ct) {
            // 步骤：按既定流程执行当前方法逻辑。
        if (entries.Count == 0) {
            return;
        }

        EnsureIdentifiersSafe(uniqueKeys);
        var fullTableName = BuildTargetTableFullName(entries[0].TargetLogicalTable, entries[0].ShardSuffix);
        var tempTableName = $"#sync_delete_{Guid.NewGuid():N}";
        await using (var createCommand = connection.CreateCommand()) {
            createCommand.Transaction = transaction;
            createCommand.CommandText = $@"
SELECT TOP (0) {string.Join(", ", uniqueKeys.Select(key => $"[{EscapeIdentifier(key)}]"))}
INTO {tempTableName}
FROM {fullTableName};";
            await createCommand.ExecuteNonQueryAsync(ct);
        }

        try {
            var parsedRows = entries
                .Select(entry => ParseBusinessKey(uniqueKeys, entry.BusinessKey))
                .ToArray();
            var dataTable = new DataTable();
            foreach (var key in uniqueKeys) {
                dataTable.Columns.Add(key, ResolveBusinessKeyColumnType(parsedRows, key));
            }

            foreach (var parsedKeyValues in parsedRows) {
                var row = dataTable.NewRow();
                foreach (var key in uniqueKeys) {
                    row[key] = parsedKeyValues.TryGetValue(key, out var value)
                        ? ConvertToDbValue(value)
                        : DBNull.Value;
                }

                dataTable.Rows.Add(row);
            }

            using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)) {
                bulkCopy.DestinationTableName = tempTableName;
                foreach (var key in uniqueKeys) {
                    bulkCopy.ColumnMappings.Add(key, key);
                }

                await bulkCopy.WriteToServerAsync(dataTable, ct);
            }

            var predicates = uniqueKeys
                .Select(key => $"target.[{EscapeIdentifier(key)}] = source.[{EscapeIdentifier(key)}]")
                .ToArray();
            await using var deleteCommand = connection.CreateCommand();
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = $@"
DELETE target
FROM {fullTableName} AS target
INNER JOIN {tempTableName} AS source
    ON {string.Join(" AND ", predicates)};";
            await deleteCommand.ExecuteNonQueryAsync(ct);
        }
        finally {
            await DropTempTableAsync(connection, transaction, tempTableName);
        }
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private async Task UpsertStatesBatchAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string tableCode,
        string stateMonthToken,
        IReadOnlyList<MergeEntry> entries,
        int batchMergeSize,
        CancellationToken ct) {
            // 步骤：按既定流程执行当前方法逻辑。
        if (entries.Count == 0) {
            return;
        }

        for (var index = 0; index < entries.Count; index += batchMergeSize) {
            ct.ThrowIfCancellationRequested();
            var currentBatchLength = Math.Min(batchMergeSize, entries.Count - index);
            var currentBatch = CreateBatch(entries, index, currentBatchLength);
            var tempTableName = $"#sync_state_{Guid.NewGuid():N}";
            await using (var createCommand = connection.CreateCommand()) {
                createCommand.Transaction = transaction;
                createCommand.CommandText = $@"
CREATE TABLE {tempTableName} (
    [TableCode] NVARCHAR(128) NOT NULL,
    [BusinessKey] NVARCHAR(512) NOT NULL,
    [RowDigest] NVARCHAR(128) NOT NULL,
    [CursorLocal] DATETIME2 NULL,
    [IsSoftDeleted] BIT NOT NULL,
    [SoftDeletedTimeLocal] DATETIME2 NULL,
    [ShardSuffix] NVARCHAR(32) NOT NULL,
    [TargetLogicalTable] NVARCHAR(128) NOT NULL,
    [UpdatedTimeLocal] DATETIME2 NOT NULL
);";
                await createCommand.ExecuteNonQueryAsync(ct);
            }

            try {
                var nowLocal = DateTime.Now;
                var dataTable = new DataTable();
                dataTable.Columns.Add("TableCode", typeof(string));
                dataTable.Columns.Add("BusinessKey", typeof(string));
                dataTable.Columns.Add("RowDigest", typeof(string));
                dataTable.Columns.Add("CursorLocal", typeof(DateTime));
                dataTable.Columns.Add("IsSoftDeleted", typeof(bool));
                dataTable.Columns.Add("SoftDeletedTimeLocal", typeof(DateTime));
                dataTable.Columns.Add("ShardSuffix", typeof(string));
                dataTable.Columns.Add("TargetLogicalTable", typeof(string));
                dataTable.Columns.Add("UpdatedTimeLocal", typeof(DateTime));
                foreach (var entry in currentBatch) {
                    dataTable.Rows.Add(
                        tableCode,
                        entry.BusinessKey,
                        entry.State.RowDigest,
                        ConvertToDbValue(entry.State.CursorLocal),
                        entry.State.IsSoftDeleted,
                        ConvertToDbValue(entry.State.SoftDeletedTimeLocal),
                        entry.ShardSuffix,
                        entry.TargetLogicalTable,
                        nowLocal);
                }

                using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)) {
                    bulkCopy.DestinationTableName = tempTableName;
                    foreach (DataColumn column in dataTable.Columns) {
                        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                    }

                    await bulkCopy.WriteToServerAsync(dataTable, ct);
                }

                await using var mergeCommand = connection.CreateCommand();
                mergeCommand.Transaction = transaction;
                mergeCommand.CommandText = $@"
MERGE {GetSyncStateTableFullName(tableCode, stateMonthToken)} AS target
USING {tempTableName} AS source
ON target.[TableCode] = source.[TableCode] AND target.[BusinessKey] = source.[BusinessKey]
WHEN MATCHED THEN
    UPDATE SET
        target.[RowDigest] = source.[RowDigest],
        target.[CursorLocal] = source.[CursorLocal],
        target.[IsSoftDeleted] = source.[IsSoftDeleted],
        target.[SoftDeletedTimeLocal] = source.[SoftDeletedTimeLocal],
        target.[ShardSuffix] = source.[ShardSuffix],
        target.[TargetLogicalTable] = source.[TargetLogicalTable],
        target.[UpdatedTimeLocal] = source.[UpdatedTimeLocal]
WHEN NOT MATCHED THEN
    INSERT ([TableCode], [BusinessKey], [RowDigest], [CursorLocal], [IsSoftDeleted], [SoftDeletedTimeLocal], [ShardSuffix], [TargetLogicalTable], [UpdatedTimeLocal])
    VALUES (source.[TableCode], source.[BusinessKey], source.[RowDigest], source.[CursorLocal], source.[IsSoftDeleted], source.[SoftDeletedTimeLocal], source.[ShardSuffix], source.[TargetLogicalTable], source.[UpdatedTimeLocal]);";
                await mergeCommand.ExecuteNonQueryAsync(ct);
            }
            finally {
                await DropTempTableAsync(connection, transaction, tempTableName);
            }
        }
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static Type ResolveEntryColumnType(IReadOnlyList<MergeEntry> entries, string column) {
        // 步骤：按既定流程执行当前方法逻辑。
        foreach (var entry in entries) {
            if (!entry.Row.TryGetValue(column, out var rawValue) || rawValue is null) {
                continue;
            }

            var dbValue = ConvertToDbValue(rawValue);
            if (dbValue is not DBNull) {
                return dbValue.GetType();
            }
        }

        return typeof(string);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static Type ResolveBusinessKeyColumnType(IReadOnlyList<IReadOnlyDictionary<string, object?>> parsedRows, string column) {
        // 步骤：按既定流程执行当前方法逻辑。
        foreach (var parsedRow in parsedRows) {
            if (!parsedRow.TryGetValue(column, out var rawValue) || rawValue is null) {
                continue;
            }

            var dbValue = ConvertToDbValue(rawValue);
            if (dbValue is not DBNull) {
                return dbValue.GetType();
            }
        }

        return typeof(string);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private async Task DropTempTableAsync(SqlConnection connection, SqlTransaction transaction, string tempTableName) {
        // 步骤：按既定流程执行当前方法逻辑。
        using var tempTableCleanupCts = new CancellationTokenSource(TimeSpan.FromSeconds(TempTableCleanupTimeoutSeconds));
        try {
            await using var dropCommand = connection.CreateCommand();
            dropCommand.Transaction = transaction;
            dropCommand.CommandText = $"DROP TABLE IF EXISTS {tempTableName};";
            await dropCommand.ExecuteNonQueryAsync(tempTableCleanupCts.Token);
        }
        catch (Exception cleanupException) {
            logger.LogWarning(cleanupException, "临时表清理异常。TempTableName={TempTableName}", tempTableName);
        }
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static string BuildColumnSignature(IEnumerable<string> columns) {
        // 步骤：按既定流程执行当前方法逻辑。
        var ordered = columns
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(column => column, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return string.Join("|", ordered);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static void TrackLatestChangedEntry(
        IDictionary<string, MergeEntry> latestChangedEntries,
        IList<string> changedBusinessKeysInOrder,
        MergeEntry entry) {
            // 步骤：按既定流程执行当前方法逻辑。
        if (!latestChangedEntries.ContainsKey(entry.BusinessKey)) {
            changedBusinessKeysInOrder.Add(entry.BusinessKey);
        }

        latestChangedEntries[entry.BusinessKey] = entry;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static IReadOnlyList<MergeEntry> BuildOrderedChangedEntries(
        IReadOnlyList<string> changedBusinessKeysInOrder,
        IReadOnlyDictionary<string, MergeEntry> latestChangedEntries) {
            // 步骤：按既定流程执行当前方法逻辑。
        if (changedBusinessKeysInOrder.Count == 0) {
            return Array.Empty<MergeEntry>();
        }

        var result = new MergeEntry[changedBusinessKeysInOrder.Count];
        for (var index = 0; index < changedBusinessKeysInOrder.Count; index++) {
            var businessKey = changedBusinessKeysInOrder[index];
            result[index] = latestChangedEntries[businessKey];
        }

        return result;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static IReadOnlyList<ShardDeleteEntry> BuildShardSwitchDeletes(
        IReadOnlyList<MergeEntry> changedEntries,
        IReadOnlyDictionary<string, PersistedState> initialStates) {
            // 步骤：按既定流程执行当前方法逻辑。
        if (changedEntries.Count == 0 || initialStates.Count == 0) {
            return Array.Empty<ShardDeleteEntry>();
        }

        var deletes = new List<ShardDeleteEntry>(changedEntries.Count);
        foreach (var entry in changedEntries) {
            if (!initialStates.TryGetValue(entry.BusinessKey, out var initialState)) {
                continue;
            }

            if (string.Equals(initialState.ShardSuffix, entry.ShardSuffix, StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            deletes.Add(new ShardDeleteEntry(
                initialState.TargetLogicalTable,
                initialState.ShardSuffix,
                initialState.BusinessKey));
        }

        return deletes;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static T[] CreateBatch<T>(IReadOnlyList<T> source, int startIndex, int length) {
        // 步骤：按既定流程执行当前方法逻辑。
        var batch = new T[length];
        for (var offset = 0; offset < length; offset++) {
            batch[offset] = source[startIndex + offset];
        }

        return batch;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private async Task UpsertTargetRowAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string targetLogicalTable,
        string shardSuffix,
        IReadOnlyDictionary<string, object?> row,
        IReadOnlyList<string> uniqueKeys,
        IReadOnlySet<string> uniqueKeySet,
        CancellationToken ct) {
            // 步骤：按既定流程执行当前方法逻辑。
        var fullTableName = BuildTargetTableFullName(targetLogicalTable, shardSuffix);
        var orderedColumns = row.Keys
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        EnsureIdentifiersSafe(orderedColumns);
        EnsureUniqueKeysPresent(uniqueKeys, row);

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        var selectAssignments = new List<string>(orderedColumns.Length);
        for (var index = 0; index < orderedColumns.Length; index++) {
            var column = orderedColumns[index];
            var parameterName = $"@p{index}";
            selectAssignments.Add($"{parameterName} AS [{column}]");
            command.Parameters.AddWithValue(parameterName, ConvertToDbValue(row[column]));
        }

        var uniquePredicates = uniqueKeys.Select(key => $"target.[{key}] = source.[{key}]").ToArray();
        var updateColumns = orderedColumns
            .Where(column => !uniqueKeySet.Contains(column))
            .Select(column => $"target.[{column}] = source.[{column}]")
            .ToArray();
        var insertColumns = string.Join(", ", orderedColumns.Select(column => $"[{column}]"));
        var insertValues = string.Join(", ", orderedColumns.Select(column => $"source.[{column}]"));
        var matchedClause = updateColumns.Length == 0
            ? string.Empty
            : $"{Environment.NewLine}WHEN MATCHED THEN{Environment.NewLine}    UPDATE SET {string.Join(", ", updateColumns)}";
        command.CommandText = $@"
MERGE {fullTableName} AS target
USING (SELECT {string.Join(", ", selectAssignments)}) AS source
ON {string.Join(" AND ", uniquePredicates)}{matchedClause}
WHEN NOT MATCHED THEN
    INSERT ({insertColumns}) VALUES ({insertValues});";
        await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private async Task DeleteTargetRowByBusinessKeyAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string targetLogicalTable,
        string shardSuffix,
        IReadOnlyList<string> uniqueKeys,
        string businessKey,
        CancellationToken ct) {
            // 步骤：按既定流程执行当前方法逻辑。
        EnsureIdentifiersSafe(uniqueKeys);
        var keyValues = ParseBusinessKey(uniqueKeys, businessKey);
        var fullTableName = BuildTargetTableFullName(targetLogicalTable, shardSuffix);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        var whereClauses = new List<string>(uniqueKeys.Count);
        for (var index = 0; index < uniqueKeys.Count; index++) {
            var key = uniqueKeys[index];
            var parameterName = $"@k{index}";
            whereClauses.Add($"[{EscapeIdentifier(key)}] = {parameterName}");
            command.Parameters.AddWithValue(parameterName, ConvertToDbValue(keyValues[key]));
        }

        command.CommandText = $"DELETE FROM {fullTableName} WHERE {string.Join(" AND ", whereClauses)};";
        await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private async Task UpsertStateAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string tableCode,
        string stateMonthToken,
        MergeEntry entry,
        CancellationToken ct) {
            // 步骤：按既定流程执行当前方法逻辑。
        var nowLocal = DateTime.Now;
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $@"
MERGE {GetSyncStateTableFullName(tableCode, stateMonthToken)} AS target
USING (SELECT
    @tableCode AS [TableCode],
    @businessKey AS [BusinessKey],
    @rowDigest AS [RowDigest],
    @cursorLocal AS [CursorLocal],
    @isSoftDeleted AS [IsSoftDeleted],
    @softDeletedTimeLocal AS [SoftDeletedTimeLocal],
    @shardSuffix AS [ShardSuffix],
    @targetLogicalTable AS [TargetLogicalTable],
    @updatedTimeLocal AS [UpdatedTimeLocal]) AS source
ON target.[TableCode] = source.[TableCode] AND target.[BusinessKey] = source.[BusinessKey]
WHEN MATCHED THEN
    UPDATE SET
        target.[RowDigest] = source.[RowDigest],
        target.[CursorLocal] = source.[CursorLocal],
        target.[IsSoftDeleted] = source.[IsSoftDeleted],
        target.[SoftDeletedTimeLocal] = source.[SoftDeletedTimeLocal],
        target.[ShardSuffix] = source.[ShardSuffix],
        target.[TargetLogicalTable] = source.[TargetLogicalTable],
        target.[UpdatedTimeLocal] = source.[UpdatedTimeLocal]
WHEN NOT MATCHED THEN
    INSERT ([TableCode], [BusinessKey], [RowDigest], [CursorLocal], [IsSoftDeleted], [SoftDeletedTimeLocal], [ShardSuffix], [TargetLogicalTable], [UpdatedTimeLocal])
    VALUES (source.[TableCode], source.[BusinessKey], source.[RowDigest], source.[CursorLocal], source.[IsSoftDeleted], source.[SoftDeletedTimeLocal], source.[ShardSuffix], source.[TargetLogicalTable], source.[UpdatedTimeLocal]);";
        command.Parameters.AddWithValue("@tableCode", tableCode);
        command.Parameters.AddWithValue("@businessKey", entry.BusinessKey);
        command.Parameters.AddWithValue("@rowDigest", entry.State.RowDigest);
        command.Parameters.AddWithValue("@cursorLocal", ConvertToDbValue(entry.State.CursorLocal));
        command.Parameters.AddWithValue("@isSoftDeleted", entry.State.IsSoftDeleted);
        command.Parameters.AddWithValue("@softDeletedTimeLocal", ConvertToDbValue(entry.State.SoftDeletedTimeLocal));
        command.Parameters.AddWithValue("@shardSuffix", entry.ShardSuffix);
        command.Parameters.AddWithValue("@targetLogicalTable", entry.TargetLogicalTable);
        command.Parameters.AddWithValue("@updatedTimeLocal", nowLocal);
        await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private async Task MarkSoftDeletedStateAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string tableCode,
        string businessKey,
        CancellationToken ct) {
            // 步骤：按既定流程执行当前方法逻辑。
        var stateTables = await ListSyncStateTableFullNamesAsync(tableCode, connection, transaction, ct);
        var nowLocal = DateTime.Now;
        foreach (var stateTable in stateTables) {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $@"
UPDATE {stateTable}
SET [IsSoftDeleted]=1,
    [SoftDeletedTimeLocal]=@softDeletedTimeLocal,
    [UpdatedTimeLocal]=@updatedTimeLocal
WHERE [TableCode]=@tableCode
  AND [BusinessKey]=@businessKey;";
            command.Parameters.AddWithValue("@softDeletedTimeLocal", nowLocal);
            command.Parameters.AddWithValue("@updatedTimeLocal", nowLocal);
            command.Parameters.AddWithValue("@tableCode", tableCode);
            command.Parameters.AddWithValue("@businessKey", businessKey);
            await command.ExecuteNonQueryAsync(ct);
        }
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private async Task DeleteStateAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string tableCode,
        string businessKey,
        CancellationToken ct) {
            // 步骤：按既定流程执行当前方法逻辑。
        var stateTables = await ListSyncStateTableFullNamesAsync(tableCode, connection, transaction, ct);
        foreach (var stateTable in stateTables) {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $@"DELETE FROM {stateTable} WHERE [TableCode]=@tableCode AND [BusinessKey]=@businessKey;";
            command.Parameters.AddWithValue("@tableCode", tableCode);
            command.Parameters.AddWithValue("@businessKey", businessKey);
            await command.ExecuteNonQueryAsync(ct);
        }
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private SyncTableOptions ResolveTableOptions(string tableCode) {
        // 步骤：按既定流程执行当前方法逻辑。
        if (_tableOptionsMap.TryGetValue(tableCode, out var tableOptions)) {
            return tableOptions;
        }

        throw new InvalidOperationException($"未找到同步表配置: {tableCode}");
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static string ResolveTargetLogicalTable(string tableCode, SyncTableOptions tableOptions) {
        // 步骤：按既定流程执行当前方法逻辑。
        var targetLogicalTable = LogicalTableNameNormalizer.NormalizeOrNull(tableOptions.TargetLogicalTable)
            ?? throw new InvalidOperationException($"同步表 {tableCode} 未配置 TargetLogicalTable。");
        if (!LogicalTableNameNormalizer.IsSafeSqlIdentifier(targetLogicalTable)) {
            throw new InvalidOperationException($"同步表 {tableCode} 配置的 TargetLogicalTable 非法：{targetLogicalTable}。");
        }

        return targetLogicalTable;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static IReadOnlyDictionary<string, SyncTableOptions> BuildTableOptionsMap(SyncJobOptions options) {
        // 步骤：按既定流程执行当前方法逻辑。
        return (options.Tables ?? [])
            .Where(table => !string.IsNullOrWhiteSpace(table.TableCode))
            .GroupBy(table => table.TableCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static IReadOnlyList<string> GetDistinctBusinessKeys(IReadOnlyList<MergeEntry> entries) {
        // 步骤：按既定流程执行当前方法逻辑。
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>(entries.Count);
        foreach (var entry in entries) {
            if (seen.Add(entry.BusinessKey)) {
                result.Add(entry.BusinessKey);
            }
        }

        return result;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static IReadOnlyList<string> GetDistinctBusinessKeys(IReadOnlyList<string> businessKeys) {
        // 步骤：按既定流程执行当前方法逻辑。
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>(businessKeys.Count);
        foreach (var businessKey in businessKeys) {
            if (string.IsNullOrWhiteSpace(businessKey)) {
                continue;
            }

            if (seen.Add(businessKey)) {
                result.Add(businessKey);
            }
        }

        return result;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    internal static string GetSyncStateTableFullName(string tableCode, string stateMonthToken) {
        // 步骤：按既定流程执行当前方法逻辑。
        if (!LogicalTableNameNormalizer.IsSafeSqlIdentifier(tableCode)) {
            throw new InvalidOperationException("检测到非法 TableCode 标识符，仅允许字母、数字、下划线。");
        }

        if (!IsValidStateMonthToken(stateMonthToken)) {
            throw new InvalidOperationException("检测到非法状态分表月份标记，仅允许6位数字（yyyyMM）。");
        }

        var physicalTableName = $"{SyncTargetStateTablePrefix}_{tableCode}_{stateMonthToken}";
        return $"[{EscapeIdentifier(SyncTargetStateSchema)}].[{EscapeIdentifier(physicalTableName)}]";
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static string ResolveStateMonthToken(DateTime localTime) {
        // 步骤：按既定流程执行当前方法逻辑。
        var ensuredLocalTime = EnsureLocalDateTime(localTime, "状态分表时间");
        return ensuredLocalTime.ToString("yyyyMM", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static async Task<IReadOnlyList<string>> ListSyncStateTableFullNamesAsync(
        string tableCode,
        SqlConnection connection,
        SqlTransaction? transaction,
        CancellationToken ct) {
            // 步骤：按既定流程执行当前方法逻辑。
        if (!LogicalTableNameNormalizer.IsSafeSqlIdentifier(tableCode)) {
            throw new InvalidOperationException("检测到非法 TableCode 标识符，仅允许字母、数字、下划线。");
        }

        var legacyBaseTableName = SyncTargetStateTablePrefix;
        var legacyPerTableName = $"{SyncTargetStateTablePrefix}_{tableCode}";
        var tableNamePrefix = $"{SyncTargetStateTablePrefix}_{tableCode}_";
        var likePattern = $"{EscapeLikePattern(tableNamePrefix)}%";
        var tableFullNames = new List<string>();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
SELECT QUOTENAME(s.[name]) + '.' + QUOTENAME(t.[name]) AS [FullName]
FROM sys.tables AS t
INNER JOIN sys.schemas AS s ON t.[schema_id] = s.[schema_id]
WHERE s.[name] = @schemaName
  AND (
        -- 兼容旧版单表：sync_target_state
        t.[name] = @legacyBaseTableName
        -- 兼容旧版按 TableCode 分表：sync_target_state_{tableCode}
     OR t.[name] = @legacyPerTableName
        -- 新版按 TableCode+月份分表：sync_target_state_{tableCode}_{yyyyMM}
     OR t.[name] LIKE @tableLikePattern ESCAPE '\'
      )
ORDER BY t.[name] DESC;";
        command.Parameters.AddWithValue("@schemaName", SyncTargetStateSchema);
        command.Parameters.AddWithValue("@legacyBaseTableName", legacyBaseTableName);
        command.Parameters.AddWithValue("@legacyPerTableName", legacyPerTableName);
        command.Parameters.AddWithValue("@tableLikePattern", likePattern);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) {
            tableFullNames.Add(reader.GetString(0));
        }

        return tableFullNames;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static bool IsValidStateMonthToken(string stateMonthToken) {
        // 步骤：按既定流程执行当前方法逻辑。
        if (string.IsNullOrWhiteSpace(stateMonthToken) || stateMonthToken.Length != StateMonthTokenLength) {
            return false;
        }

        for (var index = 0; index < stateMonthToken.Length; index++) {
            if (!char.IsDigit(stateMonthToken[index])) {
                return false;
            }
        }

        if (!char.IsDigit(stateMonthToken[4]) || !char.IsDigit(stateMonthToken[5])) {
            return false;
        }

        var month = ((stateMonthToken[4] - '0') * 10) + (stateMonthToken[5] - '0');
        return month is >= 1 and <= 12;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static string EscapeLikePattern(string value) {
        // 步骤：按既定流程执行当前方法逻辑。
        return value
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal)
            .Replace("[", @"\[", StringComparison.Ordinal);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static string BuildStateTableObjectName(string prefix, string tableNameRaw, string? tail = null) {
        // 步骤：按既定流程执行当前方法逻辑。
        var candidate = string.IsNullOrWhiteSpace(tail)
            ? $"{prefix}_{tableNameRaw}"
            : $"{prefix}_{tableNameRaw}_{tail}";
        if (candidate.Length <= 128) {
            return candidate;
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(candidate)).AsSpan(0, 8));
        return string.IsNullOrWhiteSpace(tail)
            ? $"{prefix}_{hash}"
            : $"{prefix}_{tail}_{hash}";
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private string BuildTargetTableFullName(string targetLogicalTable, string shardSuffix) {
        // 步骤：按既定流程执行当前方法逻辑。
        var physicalTable = $"{targetLogicalTable}{shardSuffix}";
        return $"[{EscapeIdentifier(_shardingOptions.Schema)}].[{EscapeIdentifier(physicalTable)}]";
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private string ResolveShardSuffix(DateTime? cursorLocal) {
        // 步骤：按既定流程执行当前方法逻辑。
        var nowLocal = DateTimeOffset.Now;
        var bootstrapSuffixes = AutoMigrationService.BuildBootstrapSuffixes(shardSuffixResolver, nowLocal, _shardingOptions.AutoCreateMonthsAhead);
        var effectiveCursorLocal = cursorLocal ?? nowLocal.LocalDateTime;
        var cursorWithOffset = new DateTimeOffset(EnsureLocalDateTime(effectiveCursorLocal, "游标时间"));
        var cursorSuffix = shardSuffixResolver.Resolve(cursorWithOffset);
        if (bootstrapSuffixes.Contains(cursorSuffix, StringComparer.OrdinalIgnoreCase)) {
            return cursorSuffix;
        }

        return bootstrapSuffixes[0];
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static IReadOnlyDictionary<string, object?> ParseBusinessKey(IReadOnlyList<string> uniqueKeys, string businessKey) {
        // 步骤：按既定流程执行当前方法逻辑。
        var values = businessKey.Split('|');
        if (values.Length != uniqueKeys.Count) {
            throw new InvalidOperationException($"业务键与 UniqueKeys 数量不匹配。BusinessKey={businessKey}, UniqueKeys={string.Join(",", uniqueKeys)}");
        }

        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < uniqueKeys.Count; index++) {
            var uniqueKey = uniqueKeys[index];
            var value = values[index];
            if (IsTemporalUniqueKeyName(uniqueKey)
                && SyncBusinessKeyBuilder.TryParseLocalDateTimeComponent(value, out var localDateTime)) {
                result[uniqueKey] = localDateTime;
                continue;
            }

            result[uniqueKey] = value;
        }

        return result;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static bool IsTemporalUniqueKeyName(string uniqueKeyName) {
        // 步骤：按既定流程执行当前方法逻辑。
        return uniqueKeyName.EndsWith("TIME", StringComparison.OrdinalIgnoreCase)
               || uniqueKeyName.EndsWith("_TIME", StringComparison.OrdinalIgnoreCase)
               || uniqueKeyName.EndsWith("DATE", StringComparison.OrdinalIgnoreCase)
               || uniqueKeyName.EndsWith("_DATE", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static void EnsureUniqueKeysPresent(IReadOnlyList<string> uniqueKeys, IReadOnlyDictionary<string, object?> row) {
        // 步骤：按既定流程执行当前方法逻辑。
        foreach (var uniqueKey in uniqueKeys) {
            if (!row.ContainsKey(uniqueKey)) {
                throw new InvalidOperationException($"行数据缺少唯一键列：{uniqueKey}");
            }
        }
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static void EnsureIdentifiersSafe(IEnumerable<string> identifiers) {
        // 步骤：按既定流程执行当前方法逻辑。
        foreach (var identifier in identifiers) {
            if (!LogicalTableNameNormalizer.IsSafeSqlIdentifier(identifier)) {
                throw new InvalidOperationException($"检测到非法 SQL 标识符：{identifier}");
            }
        }
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static string EscapeIdentifier(string identifier) {
        // 步骤：按既定流程执行当前方法逻辑。
        if (!LogicalTableNameNormalizer.IsSafeSqlIdentifier(identifier)) {
            throw new InvalidOperationException($"检测到非法 SQL 标识符：{identifier}");
        }

        return identifier.Replace("]", "]]", StringComparison.Ordinal);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static bool IsPersistedStateEqual(PersistedState persistedState, SyncTargetStateRow newState, string newShardSuffix) {
        // 步骤：按既定流程执行当前方法逻辑。
        return string.Equals(persistedState.RowDigest, newState.RowDigest, StringComparison.Ordinal)
               && Nullable.Equals(persistedState.CursorLocal, newState.CursorLocal)
               && persistedState.IsSoftDeleted == newState.IsSoftDeleted
               && Nullable.Equals(persistedState.SoftDeletedTimeLocal, newState.SoftDeletedTimeLocal)
               && string.Equals(persistedState.ShardSuffix, newShardSuffix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static void UpdateLastCursor(SyncMergeResult result, DateTime? cursorLocal) {
        // 步骤：按既定流程执行当前方法逻辑。
        if (!cursorLocal.HasValue) {
            return;
        }

        if (!result.LastSuccessCursorLocal.HasValue || cursorLocal.Value > result.LastSuccessCursorLocal.Value) {
            result.LastSuccessCursorLocal = cursorLocal.Value;
        }
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static SyncTargetStateRow BuildTargetStateRow(
        string businessKey,
        IReadOnlyDictionary<string, object?> row,
        string cursorColumn) {
            // 步骤：按既定流程执行当前方法逻辑。
        return new SyncTargetStateRow {
            BusinessKey = businessKey,
            RowDigest = ComputeRowDigestHash(row),
            CursorLocal = TryGetCursorLocal(row, cursorColumn),
            IsSoftDeleted = IsSoftDeleted(row),
            SoftDeletedTimeLocal = TryGetSoftDeletedTimeLocal(row),
        };
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static string ComputeRowDigestHash(IReadOnlyDictionary<string, object?> row) {
        // 步骤：按既定流程执行当前方法逻辑。
        var sortedKeys = row.Keys.ToArray();
        Array.Sort(sortedKeys, StringComparer.OrdinalIgnoreCase);
        using var incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var key in sortedKeys) {
            AppendLengthPrefixedUtf8(incrementalHash, key);
            var normalizedValue = NormalizeDigestValue(row[key]);
            AppendLengthPrefixedUtf8(incrementalHash, ConvertDigestValueToStableText(normalizedValue));
        }

        var hashBytes = incrementalHash.GetHashAndReset();
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static void AppendLengthPrefixedUtf8(IncrementalHash incrementalHash, string value) {
        // 步骤：按既定流程执行当前方法逻辑。
        var byteCount = Encoding.UTF8.GetByteCount(value);
        Span<byte> lengthPrefix = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(lengthPrefix, byteCount);
        incrementalHash.AppendData(lengthPrefix);
        var buffer = ArrayPool<byte>.Shared.Rent(byteCount);
        try {
            var written = Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, 0);
            incrementalHash.AppendData(buffer.AsSpan(0, written));
        }
        finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static string ConvertDigestValueToStableText(object? value) {
        // 步骤：按既定流程执行当前方法逻辑。
        if (value is null) {
            return "null";
        }

        if (value is string text) {
            return text;
        }

        if (value is bool booleanValue) {
            return booleanValue ? "true" : "false";
        }

        if (value is IFormattable formattable) {
            return formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        return JsonConvert.SerializeObject(value, DigestSerializerSettings);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static object? NormalizeDigestValue(object? value) {
        // 步骤：按既定流程执行当前方法逻辑。
        if (value is DateTime dateTime) {
            return EnsureLocalDateTime(dateTime, dateTime.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture))
                .ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture);
        }

        return value;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static object ConvertToDbValue(object? value) {
        // 步骤：按既定流程执行当前方法逻辑。
        if (value is null) {
            return DBNull.Value;
        }

        if (value is DateTime dateTime) {
            return EnsureLocalDateTime(dateTime, dateTime.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture));
        }

        if (value is DateTimeOffset dateTimeOffset) {
            return EnsureLocalDateTime(dateTimeOffset.DateTime, dateTimeOffset.ToString("O"));
        }

        return value;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static DateTime? TryGetCursorLocal(IReadOnlyDictionary<string, object?> row, string cursorColumn) {
        // 步骤：按既定流程执行当前方法逻辑。
        if (string.IsNullOrWhiteSpace(cursorColumn)) {
            return null;
        }

        if (!row.TryGetValue(cursorColumn, out var cursorValue) || cursorValue is null) {
            return null;
        }

        if (cursorValue is DateTime cursorDateTime) {
            return EnsureLocalDateTime(cursorDateTime, cursorDateTime.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture));
        }

        if (cursorValue is string cursorText) {
            if (ContainsOffsetOrZulu(cursorText)) {
                throw new InvalidOperationException($"不支持包含 Z 或 offset 的时间文本：{cursorText}");
            }

            if (DateTime.TryParse(
                    cursorText,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces,
                    out var parsedLocalDateTime)) {
                return EnsureLocalDateTime(parsedLocalDateTime, cursorText);
            }
        }

        return null;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static bool IsSoftDeleted(IReadOnlyDictionary<string, object?> row) {
        // 步骤：按既定流程执行当前方法逻辑。
        return row.TryGetValue(SyncColumnFilter.SoftDeleteFlagColumn, out var flagValue)
               && flagValue is bool flag
               && flag;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static DateTime? TryGetSoftDeletedTimeLocal(IReadOnlyDictionary<string, object?> row) {
        // 步骤：按既定流程执行当前方法逻辑。
        if (!row.TryGetValue(SyncColumnFilter.SoftDeleteTimeColumn, out var timeValue) || timeValue is null) {
            return null;
        }

        if (timeValue is DateTime dateTime) {
            return EnsureLocalDateTime(dateTime, dateTime.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture));
        }

        if (timeValue is string textValue) {
            if (ContainsOffsetOrZulu(textValue)) {
                throw new InvalidOperationException($"不支持包含 Z 或 offset 的时间文本：{textValue}");
            }

            if (DateTime.TryParse(
                    textValue,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces,
                    out var parsedLocalDateTime)) {
                return EnsureLocalDateTime(parsedLocalDateTime, textValue);
            }
        }

        return null;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static DateTime EnsureLocalDateTime(DateTime value, string? originalText) {
        // 步骤：按既定流程执行当前方法逻辑。
        if (value.Kind == DateTimeKind.Unspecified) {
            return DateTime.SpecifyKind(value, DateTimeKind.Local);
        }

        if (value.Kind == DateTimeKind.Local) {
            return value;
        }

        throw new InvalidOperationException($"检测到非本地时间语义，已拒绝加载：{originalText ?? value.ToString("O")}");
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static bool ContainsOffsetOrZulu(string value) {
        // 步骤：按既定流程执行当前方法逻辑。
        if (value.EndsWith("Z", StringComparison.Ordinal)) {
            return true;
        }

        var separatorIndex = value.IndexOf('T', StringComparison.Ordinal);
        if (separatorIndex < 0) {
            separatorIndex = value.IndexOf(' ', StringComparison.Ordinal);
        }

        if (separatorIndex < 0 || separatorIndex >= value.Length - 1) {
            return false;
        }

        var timePart = value[(separatorIndex + 1)..];
        for (var i = 0; i < timePart.Length; i++) {
            var current = timePart[i];
            if (current != '+' && current != '-') {
                continue;
            }

            var remainLength = timePart.Length - i;
            if (remainLength < 6) {
                continue;
            }

            var hasValidOffsetPattern = char.IsDigit(timePart[i + 1])
                                        && char.IsDigit(timePart[i + 2])
                                        && timePart[i + 3] == ':'
                                        && char.IsDigit(timePart[i + 4])
                                        && char.IsDigit(timePart[i + 5]);
            if (!hasValidOffsetPattern) {
                continue;
            }

            if (remainLength == 6 || !char.IsDigit(timePart[i + 6])) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 定义当前类型。
    /// </summary>
    private readonly record struct MergeEntry(
        string BusinessKey,
        string TargetLogicalTable,
        string ShardSuffix,
        IReadOnlyDictionary<string, object?> Row,
        SyncTargetStateRow State);

    /// <summary>
    /// 定义当前类型。
    /// </summary>
    private readonly record struct PersistedState(
        string BusinessKey,
        string RowDigest,
        DateTime? CursorLocal,
        bool IsSoftDeleted,
        DateTime? SoftDeletedTimeLocal,
        string ShardSuffix,
        string TargetLogicalTable);

    /// <summary>
    /// 定义当前类型。
    /// </summary>
    private readonly record struct TargetMergeGroupKey(
        string TargetLogicalTable,
        string ShardSuffix,
        string ColumnSignature);

    /// <summary>
    /// 定义当前类型。
    /// </summary>
    private readonly record struct TargetShardGroupKey(
        string TargetLogicalTable,
        string ShardSuffix);

    /// <summary>
    /// 定义当前类型。
    /// </summary>
    private readonly record struct ShardDeleteEntry(
        string TargetLogicalTable,
        string ShardSuffix,
        string BusinessKey);
}


