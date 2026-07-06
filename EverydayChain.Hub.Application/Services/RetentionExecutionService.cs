using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public class RetentionExecutionService(
    ISyncTaskConfigRepository taskConfigRepository,
    IShardTableResolver shardTableResolver,
    IShardRetentionRepository shardRetentionRepository,
    IReadOnlyList<RetentionLogTableOptions> logRetentionTables,
    ILogger<RetentionExecutionService> logger) : IRetentionExecutionService
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly IReadOnlyList<RetentionLogTableOptions> _logRetentionTables = logRetentionTables;

    public async Task<string> ExecuteRetentionCleanupAsync(CancellationToken ct)
    {
        var nowLocal = DateTime.Now;
        var enabledTables = await taskConfigRepository.ListEnabledAsync(ct);
        var retentionTargets = BuildRetentionTargets(enabledTables);
        var scannedCount = 0;
        var deletedCount = 0;
        var dryRunCount = 0;
        var failedCount = 0;
        foreach (var target in retentionTargets)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var physicalTables = await shardTableResolver.ListPhysicalTablesAsync(target.LogicalTableName, ct);
                var keepMonths = target.KeepMonths > 0 ? target.KeepMonths : 3;
                var thresholdMonth = new DateTime(nowLocal.Year, nowLocal.Month, 1, 0, 0, 0, DateTimeKind.Local).AddMonths(-(keepMonths - 1));
                foreach (var physicalTable in physicalTables)
                {
                    ct.ThrowIfCancellationRequested();
                    scannedCount++;
                    var shardMonth = shardTableResolver.TryParseShardMonth(physicalTable);
                    if (!shardMonth.HasValue || shardMonth.Value >= thresholdMonth)
                    {
                        continue;
                    }

                    var rollbackScript = await shardRetentionRepository.GenerateRollbackScriptAsync(
                        target.LogicalTableName,
                        physicalTable,
                        ct);
                    if (target.DryRun || !target.AllowDrop)
                    {
                        dryRunCount++;
                        logger.LogInformation(
                            "分表保留期 dry-run。TargetCode={TargetCode}, LogicalTable={LogicalTable}, PhysicalTable={PhysicalTable}, ShardMonth={ShardMonth}, KeepMonths={KeepMonths}, AllowDrop={AllowDrop}, RollbackScript={RollbackScript}",
                            target.TargetCode,
                            target.LogicalTableName,
                            physicalTable,
                            shardMonth.Value.ToString("yyyy-MM"),
                            keepMonths,
                            target.AllowDrop,
                            rollbackScript);
                        continue;
                    }

                    await shardRetentionRepository.DropShardTableAsync(target.LogicalTableName, physicalTable, rollbackScript, ct);
                    deletedCount++;
                }
            }
            catch (Exception ex)
            {
                failedCount++;
                logger.LogError(ex, "分表保留期执行失败。TargetCode={TargetCode}, LogicalTable={LogicalTable}", target.TargetCode, target.LogicalTableName);
            }
        }

        if (retentionTargets.Count == 0)
        {
            return "未配置启用保留期治理的表。";
        }

        var summary = $"RetentionCleanup完成。Scanned={scannedCount}, Deleted={deletedCount}, DryRun={dryRunCount}, Failed={failedCount}";
        logger.LogInformation(
            "分表保留期清理完成。Scanned={Scanned}, Deleted={Deleted}, DryRun={DryRun}, Failed={Failed}",
            scannedCount,
            deletedCount,
            dryRunCount,
            failedCount);
        return summary;
    }

    private List<RetentionTarget> BuildRetentionTargets(IReadOnlyList<Domain.Sync.SyncTableDefinition> enabledTables)
    {
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
                table.RetentionKeepMonths,
                table.RetentionDryRun,
                table.RetentionAllowDrop));
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

            existingLogicalTables.Add(logicalTableName);
            targets.Add(new RetentionTarget(
                $"LogTable:{logicalTableName}",
                logicalTableName,
                logTable.KeepMonths,
                logTable.DryRun,
                logTable.AllowDrop));
        }

        return targets;
    }
    /// <summary>
    /// 定义当前类型。
    /// </summary>
    private readonly record struct RetentionTarget(
        string TargetCode,
        string LogicalTableName,
        int KeepMonths,
        bool DryRun,
        bool AllowDrop);
}


