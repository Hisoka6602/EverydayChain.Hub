using System.Text;
using System.Buffers;
using System.Text.Json;
using System.Globalization;
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

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// SQL Server 落库版同步幂等合并仓储。
/// </summary>
public class SqlServerSyncUpsertRepository(
    IOptions<SyncJobOptions> syncJobOptions,
    IOptions<ShardingOptions> shardingOptions,
    IShardSuffixResolver shardSuffixResolver,
    IShardTableProvisioner shardTableProvisioner,
    IDangerZoneExecutor dangerZoneExecutor,
    ILogger<SqlServerSyncUpsertRepository> logger) : ISyncUpsertRepository {

    /// <summary>状态表名。</summary>
    private const string SyncTargetStateTableName = "sync_target_state";

    /// <summary>状态表固定 Schema。</summary>
    private const string SyncTargetStateSchema = "dbo";

    /// <summary>行摘要序列化配置（紧凑输出）。</summary>
    private static readonly JsonSerializerOptions DigestSerializerOptions = new() {
        WriteIndented = false,
    };

    /// <summary>同步配置快照。</summary>
    private readonly SyncJobOptions _syncJobOptions = syncJobOptions.Value;

    /// <summary>分表配置快照。</summary>
    private readonly ShardingOptions _shardingOptions = shardingOptions.Value;

    /// <summary>表配置缓存（按 TableCode 索引）。</summary>
    private readonly IReadOnlyDictionary<string, SyncTableOptions> _tableOptionsMap = BuildTableOptionsMap(syncJobOptions.Value);

    /// <inheritdoc/>
    public Task<SyncMergeResult> MergeFromStagingAsync(SyncMergeRequest request, CancellationToken ct) {
        if (request.UniqueKeys.Count == 0) {
            throw new InvalidOperationException($"同步表 {request.TableCode} 未配置 UniqueKeys，无法执行幂等合并。");
        }

        return dangerZoneExecutor.ExecuteAsync(
            $"sqlserver-upsert-merge-{request.TableCode}",
            token => MergeCoreAsync(request, token),
            ct);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SyncTargetStateRow>> ListTargetStateRowsAsync(string tableCode, CancellationToken ct) {
        return dangerZoneExecutor.ExecuteAsync(
            $"sqlserver-upsert-list-state-{tableCode}",
            token => ListTargetStateRowsCoreAsync(tableCode, token),
            ct);
    }

    /// <inheritdoc/>
    public Task<int> DeleteByBusinessKeysAsync(string tableCode, IReadOnlyList<string> businessKeys, DeletionPolicy deletionPolicy, CancellationToken ct) {
        if (businessKeys.Count == 0 || deletionPolicy == DeletionPolicy.Disabled) {
            return Task.FromResult(0);
        }

        return dangerZoneExecutor.ExecuteAsync(
            $"sqlserver-upsert-delete-{tableCode}",
            token => DeleteByBusinessKeysCoreAsync(tableCode, businessKeys, deletionPolicy, token),
            ct);
    }

    /// <summary>
    /// 执行合并核心逻辑（含真实落库与状态更新）。
    /// </summary>
    /// <param name="request">合并请求。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>合并结果。</returns>
    protected virtual async Task<SyncMergeResult> MergeCoreAsync(SyncMergeRequest request, CancellationToken ct) {
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
        var uniqueKeySet = new HashSet<string>(request.UniqueKeys, StringComparer.OrdinalIgnoreCase);
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
            await EnsureSyncTargetStateTableExistsAsync(connection, transaction, ct);
            var distinctBusinessKeys = GetDistinctBusinessKeys(entries);
            var states = await LoadStateMapAsync(connection, transaction, request.TableCode, distinctBusinessKeys, ct);
            foreach (var entry in entries) {
                ct.ThrowIfCancellationRequested();
                UpdateLastCursor(result, entry.State.CursorLocal);

                if (!states.TryGetValue(entry.BusinessKey, out var existingState)) {
                    await UpsertTargetRowAsync(connection, transaction, entry.TargetLogicalTable, entry.ShardSuffix, entry.Row, request.UniqueKeys, uniqueKeySet, ct);
                    await UpsertStateAsync(connection, transaction, request.TableCode, entry, ct);
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

                if (!string.Equals(existingState.ShardSuffix, entry.ShardSuffix, StringComparison.OrdinalIgnoreCase)) {
                    await DeleteTargetRowByBusinessKeyAsync(
                        connection,
                        transaction,
                        existingState.TargetLogicalTable,
                        existingState.ShardSuffix,
                        request.UniqueKeys,
                        existingState.BusinessKey,
                        ct);
                }

                await UpsertTargetRowAsync(connection, transaction, entry.TargetLogicalTable, entry.ShardSuffix, entry.Row, request.UniqueKeys, uniqueKeySet, ct);
                await UpsertStateAsync(connection, transaction, request.TableCode, entry, ct);
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

            await transaction.CommitAsync(ct);
            return result;
        }
        catch (Exception ex) {
            logger.LogError(ex, "SQL Server 幂等合并失败。TableCode={TableCode}", request.TableCode);
            try {
                // 事务回滚属于一致性收敛动作；在超时或手动取消场景下，仍优先完成回滚以避免半提交状态。
                await transaction.RollbackAsync(CancellationToken.None);
            }
            catch (Exception rollbackException) {
                logger.LogError(rollbackException, "SQL Server 幂等合并事务回滚失败。TableCode={TableCode}", request.TableCode);
            }

            throw;
        }
    }

    /// <summary>
    /// 列出目标状态核心逻辑。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>状态集合。</returns>
    protected virtual async Task<IReadOnlyList<SyncTargetStateRow>> ListTargetStateRowsCoreAsync(string tableCode, CancellationToken ct) {
        var states = new List<SyncTargetStateRow>();
        await using var connection = new SqlConnection(_shardingOptions.ConnectionString);
        await connection.OpenAsync(ct);
        await EnsureSyncTargetStateTableExistsAsync(connection, transaction: null, ct);
        await using var command = connection.CreateCommand();
        command.CommandText = $@"
SELECT [BusinessKey], [RowDigest], [CursorLocal], [IsSoftDeleted], [SoftDeletedTimeLocal]
FROM {GetSyncStateTableFullName()}
WHERE [TableCode]=@tableCode;";
        command.Parameters.AddWithValue("@tableCode", tableCode);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) {
            states.Add(new SyncTargetStateRow {
                BusinessKey = reader.GetString(0),
                RowDigest = reader.GetString(1),
                CursorLocal = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                IsSoftDeleted = reader.GetBoolean(3),
                SoftDeletedTimeLocal = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
            });
        }

        return states;
    }

    /// <summary>
    /// 按业务键删除核心逻辑。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    /// <param name="businessKeys">业务键集合。</param>
    /// <param name="deletionPolicy">删除策略。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>删除数量。</returns>
    protected virtual async Task<int> DeleteByBusinessKeysCoreAsync(string tableCode, IReadOnlyList<string> businessKeys, DeletionPolicy deletionPolicy, CancellationToken ct) {
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
                // 事务回滚属于一致性收敛动作；在超时或手动取消场景下，仍优先完成回滚以避免半提交状态。
                await transaction.RollbackAsync(CancellationToken.None);
            }
            catch (Exception rollbackException) {
                logger.LogError(rollbackException, "SQL Server 目标端删除事务回滚失败。TableCode={TableCode}, DeletionPolicy={DeletionPolicy}", tableCode, deletionPolicy);
            }

            throw;
        }
    }

    /// <summary>
    /// 构建合并条目集合。
    /// </summary>
    /// <param name="request">合并请求。</param>
    /// <param name="targetLogicalTable">目标逻辑表。</param>
    /// <returns>合并条目列表。</returns>
    private List<MergeEntry> BuildMergeEntries(SyncMergeRequest request, string targetLogicalTable) {
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
    /// 读取状态映射。
    /// </summary>
    /// <param name="connection">数据库连接。</param>
    /// <param name="transaction">事务对象。</param>
    /// <param name="tableCode">表编码。</param>
    /// <param name="businessKeys">已去重业务键集合（按首次出现顺序）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>状态映射。</returns>
    private async Task<Dictionary<string, PersistedState>> LoadStateMapAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string tableCode,
        IReadOnlyList<string> businessKeys,
        CancellationToken ct) {
        var stateMap = new Dictionary<string, PersistedState>(StringComparer.OrdinalIgnoreCase);
        if (businessKeys.Count == 0) {
            return stateMap;
        }

        // SQL Server 单条语句参数上限为 2100；本查询除业务键参数外还包含 tableCode 参数，因此分块上限取 900。
        const int chunkSize = 900;
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
SELECT [BusinessKey], [RowDigest], [CursorLocal], [IsSoftDeleted], [SoftDeletedTimeLocal], [ShardSuffix], [TargetLogicalTable]
FROM {GetSyncStateTableFullName()}
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
                stateMap[persisted.BusinessKey] = persisted;
            }
        }

        return stateMap;
    }

    /// <summary>
    /// 确保幂等状态表存在。
    /// </summary>
    /// <remarks>
    /// 线上若存在“迁移历史被重置 / 部分迁移缺失”场景，状态表可能未被创建。
    /// 此处进行幂等兜底，避免同步任务因 208 异常中断。
    /// </remarks>
    /// <param name="connection">数据库连接。</param>
    /// <param name="transaction">事务对象（可空）。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task EnsureSyncTargetStateTableExistsAsync(SqlConnection connection, SqlTransaction? transaction, CancellationToken ct) {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $@"
IF OBJECT_ID(N'{GetSyncStateTableFullName()}', N'U') IS NULL
BEGIN
    CREATE TABLE {GetSyncStateTableFullName()} (
        [TableCode] NVARCHAR(128) NOT NULL,
        [BusinessKey] NVARCHAR(512) NOT NULL,
        [RowDigest] NVARCHAR(128) NOT NULL,
        [CursorLocal] DATETIME2 NULL,
        [IsSoftDeleted] BIT NOT NULL,
        [SoftDeletedTimeLocal] DATETIME2 NULL,
        [ShardSuffix] NVARCHAR(32) NOT NULL,
        [TargetLogicalTable] NVARCHAR(128) NOT NULL,
        [UpdatedTimeLocal] DATETIME2 NOT NULL,
        CONSTRAINT [PK_sync_target_state] PRIMARY KEY ([TableCode], [BusinessKey])
    );

    CREATE INDEX [IX_sync_target_state_TableCode_CursorLocal]
    ON {GetSyncStateTableFullName()} ([TableCode], [CursorLocal]);
END;";
        await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// 执行目标表 UPSERT。
    /// </summary>
    /// <param name="connection">数据库连接。</param>
    /// <param name="transaction">事务对象。</param>
    /// <param name="targetLogicalTable">目标逻辑表。</param>
    /// <param name="shardSuffix">分表后缀。</param>
    /// <param name="row">数据行。</param>
    /// <param name="uniqueKeys">唯一键集合。</param>
    /// <param name="uniqueKeySet">由 <paramref name="uniqueKeys"/> 预构建的查找缓存，用于热路径 O(1) 判断更新列。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task UpsertTargetRowAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string targetLogicalTable,
        string shardSuffix,
        IReadOnlyDictionary<string, object?> row,
        IReadOnlyList<string> uniqueKeys,
        IReadOnlySet<string> uniqueKeySet,
        CancellationToken ct) {
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
    /// 删除目标物理行（按业务键拆解唯一键）。
    /// </summary>
    /// <param name="connection">数据库连接。</param>
    /// <param name="transaction">事务对象。</param>
    /// <param name="targetLogicalTable">目标逻辑表。</param>
    /// <param name="shardSuffix">分表后缀。</param>
    /// <param name="uniqueKeys">唯一键列集合。</param>
    /// <param name="businessKey">业务键文本。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task DeleteTargetRowByBusinessKeyAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string targetLogicalTable,
        string shardSuffix,
        IReadOnlyList<string> uniqueKeys,
        string businessKey,
        CancellationToken ct) {
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
    /// 插入或更新状态行。
    /// </summary>
    /// <param name="connection">数据库连接。</param>
    /// <param name="transaction">事务对象。</param>
    /// <param name="tableCode">表编码。</param>
    /// <param name="entry">合并条目。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task UpsertStateAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string tableCode,
        MergeEntry entry,
        CancellationToken ct) {
        var nowLocal = DateTime.Now;
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $@"
MERGE {GetSyncStateTableFullName()} AS target
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
    /// 软删除状态行。
    /// </summary>
    /// <param name="connection">数据库连接。</param>
    /// <param name="transaction">事务对象。</param>
    /// <param name="tableCode">表编码。</param>
    /// <param name="businessKey">业务键。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task MarkSoftDeletedStateAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string tableCode,
        string businessKey,
        CancellationToken ct) {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $@"
UPDATE {GetSyncStateTableFullName()}
SET [IsSoftDeleted]=1,
    [SoftDeletedTimeLocal]=@softDeletedTimeLocal,
    [UpdatedTimeLocal]=@updatedTimeLocal
WHERE [TableCode]=@tableCode
  AND [BusinessKey]=@businessKey;";
        command.Parameters.AddWithValue("@softDeletedTimeLocal", DateTime.Now);
        command.Parameters.AddWithValue("@updatedTimeLocal", DateTime.Now);
        command.Parameters.AddWithValue("@tableCode", tableCode);
        command.Parameters.AddWithValue("@businessKey", businessKey);
        await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// 删除状态行。
    /// </summary>
    /// <param name="connection">数据库连接。</param>
    /// <param name="transaction">事务对象。</param>
    /// <param name="tableCode">表编码。</param>
    /// <param name="businessKey">业务键。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task DeleteStateAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string tableCode,
        string businessKey,
        CancellationToken ct) {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $@"DELETE FROM {GetSyncStateTableFullName()} WHERE [TableCode]=@tableCode AND [BusinessKey]=@businessKey;";
        command.Parameters.AddWithValue("@tableCode", tableCode);
        command.Parameters.AddWithValue("@businessKey", businessKey);
        await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// 解析目标表配置。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    /// <returns>表配置。</returns>
    private SyncTableOptions ResolveTableOptions(string tableCode) {
        if (_tableOptionsMap.TryGetValue(tableCode, out var tableOptions)) {
            return tableOptions;
        }

        throw new InvalidOperationException($"未找到同步表配置: {tableCode}");
    }

    /// <summary>
    /// 解析目标逻辑表名。
    /// </summary>
    /// <param name="tableCode">表编码。</param>
    /// <param name="tableOptions">表配置。</param>
    /// <returns>目标逻辑表。</returns>
    private static string ResolveTargetLogicalTable(string tableCode, SyncTableOptions tableOptions) {
        var targetLogicalTable = LogicalTableNameNormalizer.NormalizeOrNull(tableOptions.TargetLogicalTable)
            ?? throw new InvalidOperationException($"同步表 {tableCode} 未配置 TargetLogicalTable。");
        if (!LogicalTableNameNormalizer.IsSafeSqlIdentifier(targetLogicalTable)) {
            throw new InvalidOperationException($"同步表 {tableCode} 配置的 TargetLogicalTable 非法：{targetLogicalTable}。");
        }

        return targetLogicalTable;
    }

    /// <summary>
    /// 构建表配置映射。
    /// </summary>
    /// <param name="options">同步配置。</param>
    /// <returns>映射字典。</returns>
    private static IReadOnlyDictionary<string, SyncTableOptions> BuildTableOptionsMap(SyncJobOptions options) {
        return (options.Tables ?? [])
            .Where(table => !string.IsNullOrWhiteSpace(table.TableCode))
            .GroupBy(table => table.TableCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 提取并去重业务键集合（保持输入顺序）。
    /// </summary>
    /// <remarks>
    /// 该方法用于在批次开始阶段完成一次性去重，避免在分块查询状态时重复执行 LINQ 链式分配与遍历。
    /// 保持输入顺序可保证分块参数顺序稳定，便于问题排查与性能观测结果对齐。
    /// </remarks>
    /// <param name="entries">合并条目。</param>
    /// <returns>去重后的业务键数组。</returns>
    private static IReadOnlyList<string> GetDistinctBusinessKeys(IReadOnlyList<MergeEntry> entries) {
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
    /// 提取并去重业务键集合（保持输入顺序）。
    /// </summary>
    /// <param name="businessKeys">业务键集合。</param>
    /// <returns>去重后的业务键数组。</returns>
    private static IReadOnlyList<string> GetDistinctBusinessKeys(IReadOnlyList<string> businessKeys) {
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
    /// 构建状态表全限定名。
    /// </summary>
    /// <returns>全限定表名。</returns>
    private string GetSyncStateTableFullName() {
        return $"[{EscapeIdentifier(SyncTargetStateSchema)}].[{EscapeIdentifier(SyncTargetStateTableName)}]";
    }

    /// <summary>
    /// 构建目标物理分表全限定名。
    /// </summary>
    /// <param name="targetLogicalTable">目标逻辑表。</param>
    /// <param name="shardSuffix">分表后缀。</param>
    /// <returns>全限定表名。</returns>
    private string BuildTargetTableFullName(string targetLogicalTable, string shardSuffix) {
        var physicalTable = $"{targetLogicalTable}{shardSuffix}";
        return $"[{EscapeIdentifier(_shardingOptions.Schema)}].[{EscapeIdentifier(physicalTable)}]";
    }

    /// <summary>
    /// 解析分表后缀。
    /// </summary>
    /// <param name="cursorLocal">游标时间。</param>
    /// <returns>后缀文本。</returns>
    private string ResolveShardSuffix(DateTime? cursorLocal) {
        var nowLocal = DateTimeOffset.Now;
        var bootstrapSuffixes = AutoMigrationService.BuildBootstrapSuffixes(shardSuffixResolver, nowLocal, _shardingOptions.AutoCreateMonthsAhead);
        var effectiveCursorLocal = cursorLocal ?? nowLocal.LocalDateTime;
        var cursorWithOffset = new DateTimeOffset(EnsureLocalDateTime(effectiveCursorLocal, "游标时间"));
        var cursorSuffix = shardSuffixResolver.Resolve(cursorWithOffset);
        if (bootstrapSuffixes.Contains(cursorSuffix, StringComparer.OrdinalIgnoreCase)) {
            return cursorSuffix;
        }

        // 分表归档策略按“当前已预建窗口”收敛；超出窗口的历史数据回灌到当前分表，避免写入不存在的历史分表。
        return bootstrapSuffixes[0];
    }

    /// <summary>
    /// 解析业务键值映射。
    /// </summary>
    /// <param name="uniqueKeys">唯一键集合。</param>
    /// <param name="businessKey">业务键文本。</param>
    /// <returns>键值映射。</returns>
    private static IReadOnlyDictionary<string, object?> ParseBusinessKey(IReadOnlyList<string> uniqueKeys, string businessKey) {
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
    /// 判断唯一键列名是否为时间语义。
    /// </summary>
    /// <param name="uniqueKeyName">唯一键列名。</param>
    /// <returns>时间语义返回 <c>true</c>。</returns>
    private static bool IsTemporalUniqueKeyName(string uniqueKeyName) {
        return uniqueKeyName.EndsWith("TIME", StringComparison.OrdinalIgnoreCase)
               || uniqueKeyName.EndsWith("_TIME", StringComparison.OrdinalIgnoreCase)
               || uniqueKeyName.EndsWith("DATE", StringComparison.OrdinalIgnoreCase)
               || uniqueKeyName.EndsWith("_DATE", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 校验唯一键列是否都存在于行数据。
    /// </summary>
    /// <param name="uniqueKeys">唯一键集合。</param>
    /// <param name="row">行数据。</param>
    private static void EnsureUniqueKeysPresent(IReadOnlyList<string> uniqueKeys, IReadOnlyDictionary<string, object?> row) {
        foreach (var uniqueKey in uniqueKeys) {
            if (!row.ContainsKey(uniqueKey)) {
                throw new InvalidOperationException($"行数据缺少唯一键列：{uniqueKey}");
            }
        }
    }

    /// <summary>
    /// 校验标识符集合安全性。
    /// </summary>
    /// <param name="identifiers">标识符集合。</param>
    private static void EnsureIdentifiersSafe(IEnumerable<string> identifiers) {
        foreach (var identifier in identifiers) {
            if (!LogicalTableNameNormalizer.IsSafeSqlIdentifier(identifier)) {
                throw new InvalidOperationException($"检测到非法 SQL 标识符：{identifier}");
            }
        }
    }

    /// <summary>
    /// 转义 SQL 标识符中的闭括号。
    /// </summary>
    /// <param name="identifier">原始标识符。</param>
    /// <returns>转义结果。</returns>
    private static string EscapeIdentifier(string identifier) {
        if (!LogicalTableNameNormalizer.IsSafeSqlIdentifier(identifier)) {
            throw new InvalidOperationException($"检测到非法 SQL 标识符：{identifier}");
        }

        return identifier.Replace("]", "]]", StringComparison.Ordinal);
    }

    /// <summary>
    /// 判断状态是否一致。
    /// </summary>
    /// <param name="persistedState">已持久化状态。</param>
    /// <param name="newState">新状态。</param>
    /// <param name="newShardSuffix">新后缀。</param>
    /// <returns>一致返回 <c>true</c>。</returns>
    private static bool IsPersistedStateEqual(PersistedState persistedState, SyncTargetStateRow newState, string newShardSuffix) {
        return string.Equals(persistedState.RowDigest, newState.RowDigest, StringComparison.Ordinal)
               && Nullable.Equals(persistedState.CursorLocal, newState.CursorLocal)
               && persistedState.IsSoftDeleted == newState.IsSoftDeleted
               && Nullable.Equals(persistedState.SoftDeletedTimeLocal, newState.SoftDeletedTimeLocal)
               && string.Equals(persistedState.ShardSuffix, newShardSuffix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 更新最大成功游标。
    /// </summary>
    /// <param name="result">合并结果。</param>
    /// <param name="cursorLocal">游标时间。</param>
    private static void UpdateLastCursor(SyncMergeResult result, DateTime? cursorLocal) {
        if (!cursorLocal.HasValue) {
            return;
        }

        if (!result.LastSuccessCursorLocal.HasValue || cursorLocal.Value > result.LastSuccessCursorLocal.Value) {
            result.LastSuccessCursorLocal = cursorLocal.Value;
        }
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
        string cursorColumn) {
        return new SyncTargetStateRow {
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
    private static string ComputeRowDigestHash(IReadOnlyDictionary<string, object?> row) {
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
    /// 将字符串按“长度前缀 + UTF-8 内容”追加到增量哈希，避免分隔符冲突。
    /// </summary>
    /// <param name="incrementalHash">增量哈希实例。</param>
    /// <param name="value">待追加字符串。</param>
    private static void AppendLengthPrefixedUtf8(IncrementalHash incrementalHash, string value) {
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
    /// 将归一化值转换为稳定文本。
    /// </summary>
    /// <param name="value">归一化值。</param>
    /// <returns>稳定文本。</returns>
    private static string ConvertDigestValueToStableText(object? value) {
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

        return JsonSerializer.Serialize(value, DigestSerializerOptions);
    }

    /// <summary>
    /// 归一化摘要计算值。
    /// </summary>
    /// <param name="value">原始值。</param>
    /// <returns>归一化值。</returns>
    private static object? NormalizeDigestValue(object? value) {
        if (value is DateTime dateTime) {
            return EnsureLocalDateTime(dateTime, dateTime.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture))
                .ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture);
        }

        return value;
    }

    /// <summary>
    /// 转换数据库参数值。
    /// </summary>
    /// <param name="value">原始值。</param>
    /// <returns>参数值。</returns>
    private static object ConvertToDbValue(object? value) {
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
    /// 尝试提取游标本地时间。
    /// </summary>
    /// <param name="row">业务行。</param>
    /// <param name="cursorColumn">游标列名。</param>
    /// <returns>游标本地时间。</returns>
    private static DateTime? TryGetCursorLocal(IReadOnlyDictionary<string, object?> row, string cursorColumn) {
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
    /// 判断业务行是否为软删除。
    /// </summary>
    /// <param name="row">业务行。</param>
    /// <returns>软删除返回 <c>true</c>。</returns>
    private static bool IsSoftDeleted(IReadOnlyDictionary<string, object?> row) {
        return row.TryGetValue(SyncColumnFilter.SoftDeleteFlagColumn, out var flagValue)
               && flagValue is bool flag
               && flag;
    }

    /// <summary>
    /// 尝试提取软删除本地时间。
    /// </summary>
    /// <param name="row">业务行。</param>
    /// <returns>软删除时间。</returns>
    private static DateTime? TryGetSoftDeletedTimeLocal(IReadOnlyDictionary<string, object?> row) {
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
    /// 确保时间值满足本地时间语义。
    /// </summary>
    /// <param name="value">时间值。</param>
    /// <param name="originalText">原始文本。</param>
    /// <returns>本地语义时间值。</returns>
    private static DateTime EnsureLocalDateTime(DateTime value, string? originalText) {
        if (value.Kind == DateTimeKind.Unspecified) {
            return DateTime.SpecifyKind(value, DateTimeKind.Local);
        }

        if (value.Kind == DateTimeKind.Local) {
            return value;
        }

        throw new InvalidOperationException($"检测到非本地时间语义，已拒绝加载：{originalText ?? value.ToString("O")}");
    }

    /// <summary>
    /// 判断时间文本是否包含 Z 或 offset 信息。
    /// </summary>
    /// <param name="value">时间文本。</param>
    /// <returns>包含则返回 <c>true</c>。</returns>
    private static bool ContainsOffsetOrZulu(string value) {
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
    /// 合并条目。
    /// </summary>
    /// <param name="BusinessKey">业务键。</param>
    /// <param name="TargetLogicalTable">目标逻辑表。</param>
    /// <param name="ShardSuffix">分表后缀。</param>
    /// <param name="Row">行数据。</param>
    /// <param name="State">状态行。</param>
    private readonly record struct MergeEntry(
        string BusinessKey,
        string TargetLogicalTable,
        string ShardSuffix,
        IReadOnlyDictionary<string, object?> Row,
        SyncTargetStateRow State);

    /// <summary>
    /// 持久化状态快照。
    /// </summary>
    /// <param name="BusinessKey">业务键。</param>
    /// <param name="RowDigest">行摘要。</param>
    /// <param name="CursorLocal">游标时间。</param>
    /// <param name="IsSoftDeleted">软删除标记。</param>
    /// <param name="SoftDeletedTimeLocal">软删除时间。</param>
    /// <param name="ShardSuffix">分表后缀。</param>
    /// <param name="TargetLogicalTable">目标逻辑表。</param>
    private readonly record struct PersistedState(
        string BusinessKey,
        string RowDigest,
        DateTime? CursorLocal,
        bool IsSoftDeleted,
        DateTime? SoftDeletedTimeLocal,
        string ShardSuffix,
        string TargetLogicalTable);
}
