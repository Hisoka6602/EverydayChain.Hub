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
/// Oracle 远端状态回写器。
/// 仅按 <c>ROWID</c> 更新状态列，禁止主键条件更新。
/// </summary>
public class OracleRemoteStatusWriter(
    IOptions<OracleOptions> oracleOptions,
    IDangerZoneExecutor dangerZoneExecutor,
    ILogger<OracleRemoteStatusWriter> logger) : IOracleRemoteStatusWriter {

    /// <summary>默认命令超时秒数。</summary>
    private const int DefaultCommandTimeoutSeconds = 60;

    /// <summary>生效连接字符串。</summary>
    private readonly string _effectiveConnectionString = BuildConnectionString(oracleOptions.Value);

    /// <summary>Oracle 配置快照。</summary>
    private readonly OracleOptions _options = oracleOptions.Value;

    /// <inheritdoc/>
    public async Task<int> WriteBackByRowIdAsync(
        SyncTableDefinition definition,
        RemoteStatusConsumeProfile profile,
        string batchId,
        IReadOnlyList<string> rowIds,
        CancellationToken ct) {
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
    /// 命令超时解析。
    /// </summary>
    /// <returns>超时秒数。</returns>
    private int ResolveCommandTimeout() {
        return _options.CommandTimeoutSeconds > 0 ? _options.CommandTimeoutSeconds : DefaultCommandTimeoutSeconds;
    }

    /// <summary>
    /// 构建连接串。
    /// </summary>
    /// <param name="options">Oracle 配置。</param>
    /// <returns>连接串。</returns>
    private static string BuildConnectionString(OracleOptions options) {
        return OracleConnectionStringResolver.BuildEffectiveConnectionString(options);
    }

    /// <summary>
    /// 标识符安全校验。
    /// </summary>
    /// <param name="identifier">标识符。</param>
    /// <param name="fieldName">字段名。</param>
    private static void EnsureSafeIdentifier(string identifier, string fieldName) {
        if (string.IsNullOrWhiteSpace(identifier) || !identifier.All(ch => char.IsLetterOrDigit(ch) || ch == '_')) {
            throw new InvalidOperationException($"{fieldName} 包含非法字符，仅允许字母、数字、下划线。 ");
        }
    }

    /// <summary>
    /// 创建指定长度并填充相同值的数组，避免 LINQ 分配开销。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    /// <param name="value">填充值。</param>
    /// <param name="count">数组长度。</param>
    /// <returns>填充后的数组。</returns>
    private static T[] FillArray<T>(T value, int count) {
        var arr = new T[count];
        Array.Fill(arr, value);
        return arr;
    }
}
