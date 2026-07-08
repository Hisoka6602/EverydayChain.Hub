using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Domain.Aggregates.AuditLogs;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 提供分表和固定表的保留期治理能力。
/// 该服务会在执行前先抢占运行租约，随后对每个清理目标分别落审计并执行删除，确保无人值守场景下具备互斥和可追溯能力。
/// </summary>
public class RetentionExecutionService(
    ISyncTaskConfigRepository taskConfigRepository,
    IShardTableResolver shardTableResolver,
    IShardRetentionRepository shardRetentionRepository,
    IRuntimeLeaseRepository runtimeLeaseRepository,
    IRetentionCleanupAuditLogRepository retentionCleanupAuditLogRepository,
    IReadOnlyList<RetentionLogTableOptions> logRetentionTables,
    IOptions<RetentionJobOptions> retentionJobOptions,
    ILogger<RetentionExecutionService> logger) : IRetentionExecutionService
{
    /// <summary>
    /// 存储保留期任务租约键。
    /// </summary>
    private const string RetentionLeaseKey = "retention-cleanup";

    /// <summary>
    /// 存储保留期任务最小租约秒数。
    /// </summary>
    private const int MinimumLeaseSeconds = 900;

    /// <summary>
    /// 存储保留期任务最大租约秒数。
    /// </summary>
    private const int MaximumLeaseSeconds = 86400;

    /// <summary>
    /// 存储日志表与固定表保留期配置集合。
    /// </summary>
    private readonly IReadOnlyList<RetentionLogTableOptions> _logRetentionTables = logRetentionTables;

    /// <summary>
    /// 存储保留期任务配置。
    /// </summary>
    private readonly RetentionJobOptions _retentionJobOptions = retentionJobOptions.Value;

    /// <summary>
    /// 存储当前实例的租约持有者标识。
    /// </summary>
    private readonly string _leaseOwnerId = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";

    /// <summary>
    /// 执行一轮保留期治理。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>本轮执行摘要。</returns>
    public async Task<string> ExecuteRetentionCleanupAsync(CancellationToken ct)
    {
        // 步骤：先构建本轮目标集合；若存在目标则抢占全局租约，随后逐目标审计并执行清理，最后释放租约。
        var enabledTables = await taskConfigRepository.ListEnabledAsync(ct);
        var retentionTargets = BuildRetentionTargets(enabledTables);
        if (retentionTargets.Count == 0)
        {
            return "未配置启用保留期治理的表。";
        }

        var leaseSeconds = ResolveLeaseSeconds();
        var acquiredTimeLocal = DateTime.Now;
        var acquired = await runtimeLeaseRepository.TryAcquireAsync(
            RetentionLeaseKey,
            _leaseOwnerId,
            acquiredTimeLocal,
            acquiredTimeLocal.AddSeconds(leaseSeconds),
            ct);
        if (!acquired)
        {
            logger.LogWarning("保留期治理跳过执行，原因是已有其他实例持有运行租约。LeaseKey={LeaseKey}", RetentionLeaseKey);
            return "RetentionCleanup跳过，已有其他实例正在执行。";
        }

        var batchId = Guid.NewGuid().ToString("N");
        var scannedCount = 0;
        var deletedCount = 0;
        var dryRunCount = 0;
        var failedCount = 0;

        try
        {
            foreach (var target in retentionTargets)
            {
                ct.ThrowIfCancellationRequested();
                RetentionCleanupAuditLogEntity? auditLog = null;
                try
                {
                    var startedTimeLocal = DateTime.Now;
                    var thresholdTimeLocal = BuildThresholdMonth(startedTimeLocal, target.KeepMonths > 0 ? target.KeepMonths : 3);
                    auditLog = BuildAuditLog(batchId, target, thresholdTimeLocal, startedTimeLocal);
                    await retentionCleanupAuditLogRepository.SaveAsync(auditLog, ct);

                    var result = target.RetentionMode switch
                    {
                        RetentionMode.DropShards => await ExecuteDropShardRetentionAsync(target, thresholdTimeLocal, ct),
                        RetentionMode.DeleteRows => await ExecuteDeleteRowsRetentionAsync(target, thresholdTimeLocal, ct),
                        _ => throw new InvalidOperationException($"保留期配置包含未知模式。TargetCode={target.TargetCode}, LogicalTable={target.LogicalTableName}, RetentionMode={target.RetentionMode}")
                    };

                    scannedCount += result.ScannedCount;
                    deletedCount += result.DeletedCount;
                    dryRunCount += result.DryRunCount;
                    await retentionCleanupAuditLogRepository.UpdateResultAsync(
                        auditLog.Id,
                        executionStage: "Completed",
                        scannedCount: result.ScannedCount,
                        candidateCount: result.CandidateCount,
                        deletedCount: result.DeletedCount,
                        message: result.Message,
                        completedTimeLocal: DateTime.Now,
                        ct);
                }
                catch (Exception ex)
                {
                    failedCount++;
                    logger.LogError(ex, "保留期执行失败。TargetCode={TargetCode}, LogicalTable={LogicalTable}", target.TargetCode, target.LogicalTableName);
                    if (auditLog is not null)
                    {
                        await retentionCleanupAuditLogRepository.UpdateResultAsync(
                            auditLog.Id,
                            executionStage: "Failed",
                            scannedCount: 0,
                            candidateCount: 0,
                            deletedCount: 0,
                            message: BuildFailureMessage(ex),
                            completedTimeLocal: DateTime.Now,
                            ct);
                    }
                }
            }
        }
        finally
        {
            await runtimeLeaseRepository.ReleaseAsync(RetentionLeaseKey, _leaseOwnerId, CancellationToken.None);
        }

        var summary = $"RetentionCleanup完成。Scanned={scannedCount}, Deleted={deletedCount}, DryRun={dryRunCount}, Failed={failedCount}";
        logger.LogInformation(
            "保留期清理完成。Scanned={Scanned}, Deleted={Deleted}, DryRun={DryRun}, Failed={Failed}, BatchId={BatchId}",
            scannedCount,
            deletedCount,
            dryRunCount,
            failedCount,
            batchId);
        return summary;
    }

    /// <summary>
    /// 执行旧分表删除模式。
    /// </summary>
    /// <param name="target">治理目标。</param>
    /// <param name="thresholdTimeLocal">保留阈值时间。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>目标执行结果。</returns>
    private async Task<RetentionTargetExecutionResult> ExecuteDropShardRetentionAsync(
        RetentionTarget target,
        DateTime thresholdTimeLocal,
        CancellationToken ct)
    {
        // 步骤：遍历逻辑表下的所有物理分表，识别早于阈值月份的旧分表，再按配置决定预演还是实际删除。
        var physicalTables = await shardTableResolver.ListPhysicalTablesAsync(target.LogicalTableName, ct);
        var scannedCount = 0;
        var candidateCount = 0;
        var deletedCount = 0;
        var dryRunCount = 0;

        foreach (var physicalTable in physicalTables)
        {
            ct.ThrowIfCancellationRequested();
            scannedCount++;
            var shardMonth = shardTableResolver.TryParseShardMonth(physicalTable);
            if (!shardMonth.HasValue || shardMonth.Value >= thresholdTimeLocal)
            {
                continue;
            }

            candidateCount++;
            var rollbackScript = await shardRetentionRepository.GenerateRollbackScriptAsync(
                target.LogicalTableName,
                physicalTable,
                ct);
            if (target.DryRun || !target.AllowDelete)
            {
                dryRunCount++;
                logger.LogInformation(
                    "分表保留期预演。TargetCode={TargetCode}, LogicalTable={LogicalTable}, PhysicalTable={PhysicalTable}, KeepMonths={KeepMonths}, AllowDelete={AllowDelete}, RollbackScript={RollbackScript}",
                    target.TargetCode,
                    target.LogicalTableName,
                    physicalTable,
                    target.KeepMonths,
                    target.AllowDelete,
                    rollbackScript);
                continue;
            }

            await shardRetentionRepository.DropShardTableAsync(target.LogicalTableName, physicalTable, rollbackScript, ct);
            deletedCount++;
        }

        return new RetentionTargetExecutionResult(
            scannedCount,
            candidateCount,
            deletedCount,
            dryRunCount,
            BuildDropShardsMessage(target, candidateCount, deletedCount, dryRunCount));
    }

    /// <summary>
    /// 执行固定表旧数据删行模式。
    /// </summary>
    /// <param name="target">治理目标。</param>
    /// <param name="thresholdTimeLocal">保留阈值时间。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>目标执行结果。</returns>
    private async Task<RetentionTargetExecutionResult> ExecuteDeleteRowsRetentionAsync(
        RetentionTarget target,
        DateTime thresholdTimeLocal,
        CancellationToken ct)
    {
        // 步骤：先统计固定表中过期数据行数，再根据预演开关和危险动作许可决定是否批量删除。
        if (string.IsNullOrWhiteSpace(target.TimeColumnName))
        {
            logger.LogWarning("固定表保留期配置缺少时间列，已跳过。TargetCode={TargetCode}, LogicalTable={LogicalTable}", target.TargetCode, target.LogicalTableName);
            return new RetentionTargetExecutionResult(0, 0, 0, 0, "固定表保留期配置缺少时间列，已跳过。");
        }

        var scannedCount = 1;
        var expiredRowCount = await shardRetentionRepository.CountRowsBeforeAsync(target.LogicalTableName, target.TimeColumnName, thresholdTimeLocal, ct);
        if (expiredRowCount <= 0)
        {
            return new RetentionTargetExecutionResult(scannedCount, 0, 0, 0, "未发现超出保留窗口的旧数据行。");
        }

        if (target.DryRun || !target.AllowDelete)
        {
            logger.LogInformation(
                "固定表保留期预演。TargetCode={TargetCode}, LogicalTable={LogicalTable}, TimeColumn={TimeColumn}, ExpiredRows={ExpiredRows}, KeepMonths={KeepMonths}, AllowDelete={AllowDelete}",
                target.TargetCode,
                target.LogicalTableName,
                target.TimeColumnName,
                expiredRowCount,
                target.KeepMonths,
                target.AllowDelete);
            return new RetentionTargetExecutionResult(
                scannedCount,
                expiredRowCount,
                0,
                1,
                BuildDeleteRowsMessage(target, expiredRowCount, deletedCount: 0, dryRunCount: 1));
        }

        var deletedRows = await shardRetentionRepository.DeleteRowsBeforeAsync(
            target.LogicalTableName,
            target.TimeColumnName,
            thresholdTimeLocal,
            target.DeleteBatchSize,
            ct);
        logger.LogWarning(
            "固定表保留期已删除旧数据。TargetCode={TargetCode}, LogicalTable={LogicalTable}, TimeColumn={TimeColumn}, DeletedRows={DeletedRows}, ThresholdTime={ThresholdTime}",
            target.TargetCode,
            target.LogicalTableName,
            target.TimeColumnName,
            deletedRows,
            thresholdTimeLocal.ToString("yyyy-MM-dd HH:mm:ss"));
        return new RetentionTargetExecutionResult(
            scannedCount,
            expiredRowCount,
            deletedRows,
            0,
            BuildDeleteRowsMessage(target, expiredRowCount, deletedRows, dryRunCount: 0));
    }

    /// <summary>
    /// 构建保留期治理目标集合。
    /// </summary>
    /// <param name="enabledTables">已启用同步表配置。</param>
    /// <returns>治理目标集合。</returns>
    private List<RetentionTarget> BuildRetentionTargets(IReadOnlyList<Domain.Sync.SyncTableDefinition> enabledTables)
    {
        // 步骤：先装配业务主表分表治理，再装配日志表和固定表治理，并按逻辑表名去重。
        var targets = new List<RetentionTarget>();
        var existingLogicalTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var table in enabledTables)
        {
            if (!table.RetentionEnabled)
            {
                continue;
            }

            existingLogicalTables.Add(table.TargetLogicalTable);
            targets.Add(new RetentionTarget(
                table.TableCode,
                table.TargetLogicalTable,
                RetentionMode.DropShards,
                TimeColumnName: null,
                table.RetentionKeepMonths,
                table.RetentionDryRun,
                table.RetentionAllowDrop,
                DeleteBatchSize: 0));
        }

        foreach (var logTable in _logRetentionTables)
        {
            if (!logTable.Enabled)
            {
                continue;
            }

            var logicalTableName = LogicalTableNameNormalizer.NormalizeOrNull(logTable.LogicalTableName);
            if (logicalTableName is null || !LogicalTableNameNormalizer.IsSafeSqlIdentifier(logicalTableName))
            {
                logger.LogWarning("日志表保留期配置非法，已跳过。LogicalTableName={LogicalTableName}", logTable.LogicalTableName);
                continue;
            }

            if (existingLogicalTables.Contains(logicalTableName))
            {
                continue;
            }

            var retentionMode = ParseRetentionMode(logTable.RetentionMode, logicalTableName);
            var timeColumnName = NormalizeTimeColumnName(logTable.TimeColumnName, logicalTableName, retentionMode);
            existingLogicalTables.Add(logicalTableName);
            targets.Add(new RetentionTarget(
                $"LogTable:{logicalTableName}",
                logicalTableName,
                retentionMode,
                timeColumnName,
                logTable.KeepMonths,
                logTable.DryRun,
                logTable.AllowDrop,
                logTable.DeleteBatchSize > 0 ? logTable.DeleteBatchSize : 10000));
        }

        return targets;
    }

    /// <summary>
    /// 解析保留模式。
    /// </summary>
    /// <param name="retentionModeText">配置文本。</param>
    /// <param name="logicalTableName">逻辑表名。</param>
    /// <returns>保留模式。</returns>
    private RetentionMode ParseRetentionMode(string? retentionModeText, string logicalTableName)
    {
        // 步骤：空值默认按旧分表删除处理；非法值直接阻断该配置，避免无人值守下误删。
        if (string.IsNullOrWhiteSpace(retentionModeText))
        {
            return RetentionMode.DropShards;
        }

        var normalized = retentionModeText.Trim();
        if (string.Equals(normalized, nameof(RetentionMode.DropShards), StringComparison.OrdinalIgnoreCase))
        {
            return RetentionMode.DropShards;
        }

        if (string.Equals(normalized, nameof(RetentionMode.DeleteRows), StringComparison.OrdinalIgnoreCase))
        {
            return RetentionMode.DeleteRows;
        }

        throw new InvalidOperationException($"保留期配置包含无效的 RetentionMode。LogicalTable={logicalTableName}, RetentionMode={retentionModeText}");
    }

    /// <summary>
    /// 归一化固定表删行模式的时间列配置。
    /// </summary>
    /// <param name="timeColumnNameText">配置文本。</param>
    /// <param name="logicalTableName">逻辑表名。</param>
    /// <param name="retentionMode">保留模式。</param>
    /// <returns>归一化后的时间列名。</returns>
    private string? NormalizeTimeColumnName(string? timeColumnNameText, string logicalTableName, RetentionMode retentionMode)
    {
        // 步骤：删分表模式忽略时间列；删行模式要求显式提供并且必须是安全列名。
        if (retentionMode != RetentionMode.DeleteRows)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(timeColumnNameText))
        {
            throw new InvalidOperationException($"固定表保留期配置缺少 TimeColumnName。LogicalTable={logicalTableName}");
        }

        var normalized = timeColumnNameText.Trim();
        if (!LogicalTableNameNormalizer.IsSafeSqlIdentifier(normalized))
        {
            throw new InvalidOperationException($"固定表保留期配置包含非法 TimeColumnName。LogicalTable={logicalTableName}, TimeColumnName={timeColumnNameText}");
        }

        return normalized;
    }

    /// <summary>
    /// 构建保留阈值月份起点。
    /// </summary>
    /// <param name="nowLocal">当前本地时间。</param>
    /// <param name="keepMonths">保留月数。</param>
    /// <returns>阈值月份起点。</returns>
    private static DateTime BuildThresholdMonth(DateTime nowLocal, int keepMonths)
    {
        // 步骤：保留当前月向前 keepMonths-1 个月的数据，早于该窗口起点的旧数据将成为治理候选。
        return new DateTime(nowLocal.Year, nowLocal.Month, 1, 0, 0, 0, DateTimeKind.Local).AddMonths(-(keepMonths - 1));
    }

    /// <summary>
    /// 解析租约秒数。
    /// </summary>
    /// <returns>本轮执行的租约秒数。</returns>
    private int ResolveLeaseSeconds()
    {
        // 步骤：优先使用轮询间隔作为租约上限依据，同时保证至少覆盖一轮长任务，避免多实例并发重入。
        var configuredPollingSeconds = _retentionJobOptions.PollingIntervalSeconds > 0 ? _retentionJobOptions.PollingIntervalSeconds : 3600;
        return Math.Clamp(Math.Max(configuredPollingSeconds, MinimumLeaseSeconds), MinimumLeaseSeconds, MaximumLeaseSeconds);
    }

    /// <summary>
    /// 构建目标级审计起始记录。
    /// </summary>
    /// <param name="batchId">批次标识。</param>
    /// <param name="target">治理目标。</param>
    /// <param name="thresholdTimeLocal">保留阈值时间。</param>
    /// <param name="startedTimeLocal">开始时间。</param>
    /// <returns>审计实体。</returns>
    private RetentionCleanupAuditLogEntity BuildAuditLog(
        string batchId,
        RetentionTarget target,
        DateTime thresholdTimeLocal,
        DateTime startedTimeLocal)
    {
        // 步骤：将目标配置与实例信息固化到审计记录中，后续只回写执行结果字段。
        return new RetentionCleanupAuditLogEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            BatchId = batchId,
            TargetCode = TrimToLength(target.TargetCode, 64),
            LogicalTableName = TrimToLength(target.LogicalTableName, 128),
            RetentionMode = TrimToLength(target.RetentionMode.ToString(), 32),
            TimeColumnName = TrimToLength(target.TimeColumnName ?? string.Empty, 64),
            KeepMonths = target.KeepMonths,
            IsDryRun = target.DryRun,
            AllowDelete = target.AllowDelete,
            ThresholdTimeLocal = thresholdTimeLocal,
            ExecutionStage = "Started",
            ScannedCount = 0,
            CandidateCount = 0,
            DeletedCount = 0,
            Message = "已进入保留期治理执行队列，等待目标处理结果。",
            InstanceId = TrimToLength(_leaseOwnerId, 128),
            StartedTimeLocal = startedTimeLocal,
            CompletedTimeLocal = null
        };
    }

    /// <summary>
    /// 构建分表删除模式结果说明。
    /// </summary>
    /// <param name="target">治理目标。</param>
    /// <param name="candidateCount">候选数量。</param>
    /// <param name="deletedCount">删除数量。</param>
    /// <param name="dryRunCount">预演数量。</param>
    /// <returns>结果说明。</returns>
    private static string BuildDropShardsMessage(RetentionTarget target, int candidateCount, int deletedCount, int dryRunCount)
    {
        if (candidateCount <= 0)
        {
            return "未发现超出保留窗口的旧分表。";
        }

        if (target.DryRun)
        {
            return $"预演识别到 {candidateCount} 张旧分表，未执行实际删除。";
        }

        if (!target.AllowDelete)
        {
            return $"识别到 {candidateCount} 张旧分表，但当前目标未开放实际删除。";
        }

        return $"已删除 {deletedCount}/{candidateCount} 张旧分表。";
    }

    /// <summary>
    /// 构建固定表删行模式结果说明。
    /// </summary>
    /// <param name="target">治理目标。</param>
    /// <param name="candidateCount">候选数量。</param>
    /// <param name="deletedCount">删除数量。</param>
    /// <param name="dryRunCount">预演数量。</param>
    /// <returns>结果说明。</returns>
    private static string BuildDeleteRowsMessage(RetentionTarget target, int candidateCount, int deletedCount, int dryRunCount)
    {
        if (candidateCount <= 0)
        {
            return "未发现超出保留窗口的旧数据行。";
        }

        if (dryRunCount > 0 || target.DryRun)
        {
            return $"预演识别到 {candidateCount} 行旧数据，未执行实际删除。";
        }

        if (!target.AllowDelete)
        {
            return $"识别到 {candidateCount} 行旧数据，但当前目标未开放实际删除。";
        }

        return $"已删除 {deletedCount}/{candidateCount} 行旧数据。";
    }

    /// <summary>
    /// 构建失败说明文案。
    /// </summary>
    /// <param name="ex">异常对象。</param>
    /// <returns>截断后的失败说明。</returns>
    private static string BuildFailureMessage(Exception ex)
    {
        return TrimToLength($"执行异常：{ex.Message}", 512);
    }

    /// <summary>
    /// 按指定长度截断文本。
    /// </summary>
    /// <param name="text">原始文本。</param>
    /// <param name="maxLength">最大长度。</param>
    /// <returns>截断后的文本。</returns>
    private static string TrimToLength(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
        {
            return text;
        }

        return text[..maxLength];
    }

    /// <summary>
    /// 表示保留期治理模式。
    /// </summary>
    private enum RetentionMode
    {
        /// <summary>
        /// 表示删除整张旧分表。
        /// </summary>
        DropShards,

        /// <summary>
        /// 表示按时间列删除固定表旧数据行。
        /// </summary>
        DeleteRows
    }

    /// <summary>
    /// 表示单个保留期治理目标。
    /// </summary>
    /// <param name="TargetCode">目标编码。</param>
    /// <param name="LogicalTableName">逻辑表名或固定表名。</param>
    /// <param name="RetentionMode">治理模式。</param>
    /// <param name="TimeColumnName">时间列名。</param>
    /// <param name="KeepMonths">保留月数。</param>
    /// <param name="DryRun">是否预演。</param>
    /// <param name="AllowDelete">是否允许执行删除。</param>
    /// <param name="DeleteBatchSize">删行模式单批删除行数。</param>
    private readonly record struct RetentionTarget(
        string TargetCode,
        string LogicalTableName,
        RetentionMode RetentionMode,
        string? TimeColumnName,
        int KeepMonths,
        bool DryRun,
        bool AllowDelete,
        int DeleteBatchSize);

    /// <summary>
    /// 表示单个治理目标的执行结果。
    /// </summary>
    /// <param name="ScannedCount">扫描数量。</param>
    /// <param name="CandidateCount">候选数量。</param>
    /// <param name="DeletedCount">删除数量。</param>
    /// <param name="DryRunCount">预演数量。</param>
    /// <param name="Message">结果说明。</param>
    private readonly record struct RetentionTargetExecutionResult(
        int ScannedCount,
        int CandidateCount,
        int DeletedCount,
        int DryRunCount,
        string Message);
}
