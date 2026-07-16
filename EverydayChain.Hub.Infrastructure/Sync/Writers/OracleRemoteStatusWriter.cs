using System.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EverydayChain.Hub.Domain.Sync;
using Oracle.ManagedDataAccess.Client;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Domain.Sync.Models;
using EverydayChain.Hub.Infrastructure.Services;
using EverydayChain.Hub.Application.Abstractions.Sync;

namespace EverydayChain.Hub.Infrastructure.Sync.Writers;

/// <summary>
/// 定义 OracleRemoteStatusWriter 类型。
/// </summary>
public class OracleRemoteStatusWriter(
    IOptions<OracleOptions> oracleOptions,
    IDangerZoneExecutor dangerZoneExecutor,
    ILogger<OracleRemoteStatusWriter> logger) : IOracleRemoteStatusWriter {

    /// <summary>
    /// 存储 DefaultCommandTimeoutSeconds 字段。
    /// </summary>
    private const int DefaultCommandTimeoutSeconds = 60;

    /// <summary>
    /// 保存合并配置后的 Oracle 回写连接字符串。
    /// </summary>
    private readonly string _effectiveConnectionString = BuildConnectionString(oracleOptions.Value);

    /// <summary>
    /// 存储 _options 字段。
    /// </summary>
    private readonly OracleOptions _options = oracleOptions.Value;

    /// <summary>
    /// 执行 WriteBackByRowIdAsync 方法。
    /// </summary>
    public async Task<int> WriteBackByRowIdAsync(
        SyncTableDefinition definition,
        RemoteStatusConsumeProfile profile,
        string batchId,
        IReadOnlyList<string> rowIds,
        CancellationToken ct) {
            // 步骤：执行 WriteBackByRowIdAsync 方法的核心处理流程。
        if (rowIds.Count == 0) {
            return 0;
        }

        EnsureSafeIdentifier(definition.SourceSchema, nameof(definition.SourceSchema));
        EnsureSafeIdentifier(definition.SourceTable, nameof(definition.SourceTable));
        EnsureSafeIdentifier(profile.StatusColumnName, nameof(profile.StatusColumnName));
        if (!string.IsNullOrWhiteSpace(profile.WriteBackCompletedTimeColumnName)) {
            EnsureSafeIdentifier(profile.WriteBackCompletedTimeColumnName, nameof(profile.WriteBackCompletedTimeColumnName));
        }

        if (!string.IsNullOrWhiteSpace(profile.WriteBackBatchIdColumnName)) {
            EnsureSafeIdentifier(profile.WriteBackBatchIdColumnName, nameof(profile.WriteBackBatchIdColumnName));
        }

        var setClauses = new List<string>
        {
            $"{profile.StatusColumnName} = :p_completedStatus",
        };
        if (!string.IsNullOrWhiteSpace(profile.WriteBackCompletedTimeColumnName)) {
            setClauses.Add($"{profile.WriteBackCompletedTimeColumnName} = :p_completedTimeLocal");
        }

        if (!string.IsNullOrWhiteSpace(profile.WriteBackBatchIdColumnName)) {
            setClauses.Add($"{profile.WriteBackBatchIdColumnName} = :p_batchId");
        }

        var sql = $"UPDATE {definition.SourceSchema}.{definition.SourceTable} SET {string.Join(", ", setClauses)} WHERE ROWID = :p_rowid";
        try {
            return await dangerZoneExecutor.ExecuteAsync(
                $"OracleStatusDrivenWriteBack:{definition.TableCode}",
                async token => {
                    await using var connection = new OracleConnection(_effectiveConnectionString);
                    await connection.OpenAsync(token);
                    await using var command = connection.CreateCommand();
                    command.BindByName = true;
                    command.ArrayBindCount = rowIds.Count;
                    command.CommandTimeout = ResolveCommandTimeout();
                    command.CommandText = sql;
                    command.Parameters.Add("p_completedStatus", OracleDbType.Varchar2, FillArray(profile.CompletedStatusValue, rowIds.Count), ParameterDirection.Input);
                    if (!string.IsNullOrWhiteSpace(profile.WriteBackCompletedTimeColumnName)) {
                        var completedTimeLocal = DateTime.Now;
                        command.Parameters.Add(
                            "p_completedTimeLocal",
                            OracleDbType.TimeStamp,
                            FillArray(completedTimeLocal, rowIds.Count),
                            ParameterDirection.Input);
                    }

                    if (!string.IsNullOrWhiteSpace(profile.WriteBackBatchIdColumnName)) {
                        command.Parameters.Add(
                            "p_batchId",
                            OracleDbType.Varchar2,
                            FillArray(batchId, rowIds.Count),
                            ParameterDirection.Input);
                    }

                    command.Parameters.Add("p_rowid", OracleDbType.Varchar2, rowIds.ToArray(), ParameterDirection.Input);
                    var affectedRows = await command.ExecuteNonQueryAsync(token);
                    logger.LogInformation(
                        "状态驱动远端回写完成。TableCode={TableCode}, BatchId={BatchId}, SourceTable={SourceTable}, RequestedRows={RequestedRows}, AffectedRows={AffectedRows}, CompletedTimeColumn={CompletedTimeColumn}, BatchIdColumn={BatchIdColumn}",
                        definition.TableCode,
                        batchId,
                        definition.SourceTable,
                        rowIds.Count,
                        affectedRows,
                        profile.WriteBackCompletedTimeColumnName ?? "未配置",
                        profile.WriteBackBatchIdColumnName ?? "未配置");
                    return affectedRows;
                },
                ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) {
            logger.LogWarning(
                "状态驱动远端回写已取消。TableCode={TableCode}, BatchId={BatchId}, SourceTable={SourceTable}, RequestedRows={RequestedRows}, FailureReason={FailureReason}",
                definition.TableCode,
                batchId,
                definition.SourceTable,
                rowIds.Count,
                "调用方取消令牌触发回写终止。");
            throw;
        }
        catch (Exception ex) {
            logger.LogError(
                ex,
                "状态驱动远端回写执行异常。TableCode={TableCode}, BatchId={BatchId}, SourceTable={SourceTable}, RequestedRows={RequestedRows}, FailureReason={FailureReason}",
                definition.TableCode,
                batchId,
                definition.SourceTable,
                rowIds.Count,
                ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 执行 ResolveCommandTimeout 方法。
    /// </summary>
    private int ResolveCommandTimeout() {
        // 步骤：执行 ResolveCommandTimeout 方法的核心处理流程。
        return _options.CommandTimeoutSeconds > 0 ? _options.CommandTimeoutSeconds : DefaultCommandTimeoutSeconds;
    }

    /// <summary>
    /// 执行 BuildConnectionString 方法。
    /// </summary>
    private static string BuildConnectionString(OracleOptions options) {
        // 步骤：执行 BuildConnectionString 方法的核心处理流程。
        return OracleConnectionStringResolver.BuildEffectiveConnectionString(options);
    }

    /// <summary>
    /// 执行 EnsureSafeIdentifier 方法。
    /// </summary>
    private static void EnsureSafeIdentifier(string identifier, string fieldName) {
        // 步骤：执行 EnsureSafeIdentifier 方法的核心处理流程。
        if (string.IsNullOrWhiteSpace(identifier) || !identifier.All(ch => char.IsLetterOrDigit(ch) || ch == '_')) {
            throw new InvalidOperationException($"{fieldName} 包含非法字符，仅允许字母、数字、下划线。 ");
        }
    }

    /// <summary>
    /// 执行当前业务方法。
    /// </summary>
    private static T[] FillArray<T>(T value, int count) {
        // 步骤：执行当前业务方法的核心处理流程。
        var arr = new T[count];
        Array.Fill(arr, value);
        return arr;
    }
}

