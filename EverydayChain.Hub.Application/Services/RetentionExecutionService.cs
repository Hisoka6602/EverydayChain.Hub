using EverydayChain.Hub.Application.Abstractions.Persistence;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 分表保留期执行服务实现。
/// </summary>
public class RetentionExecutionService(
    ISyncTaskConfigRepository taskConfigRepository,
    IShardTableResolver shardTableResolver,
    IShardRetentionRepository shardRetentionRepository,
    ILogger<RetentionExecutionService> logger) : IRetentionExecutionService
{
    /// <inheritdoc/>
    public async Task<string> ExecuteRetentionCleanupAsync(CancellationToken ct)
    {
        var nowLocal = DateTime.Now;
        var enabledTables = await taskConfigRepository.ListEnabledAsync(ct);
        var scannedCount = 0;
        var deletedCount = 0;
        var dryRunCount = 0;
        var failedCount = 0;
        var hasRetentionTable = false;
        foreach (var table in enabledTables)
        {
            ct.ThrowIfCancellationRequested();
            if (!table.RetentionEnabled)
            {
                continue;
            }

            hasRetentionTable = true;
            try
            {
                var physicalTables = await shardTableResolver.ListPhysicalTablesAsync(table.TargetLogicalTable, ct);
                var keepMonths = table.RetentionKeepMonths > 0 ? table.RetentionKeepMonths : 3;
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
                        table.TargetLogicalTable,
                        physicalTable,
                        ct);
                    if (table.RetentionDryRun || !table.RetentionAllowDrop)
                    {
                        dryRunCount++;
                        logger.LogInformation(
                            "分表保留期 dry-run。TableCode={TableCode}, PhysicalTable={PhysicalTable}, ShardMonth={ShardMonth}, KeepMonths={KeepMonths}, AllowDrop={AllowDrop}, RollbackScript={RollbackScript}",
                            table.TableCode,
                            physicalTable,
                            shardMonth.Value.ToString("yyyy-MM"),
                            keepMonths,
                            table.RetentionAllowDrop,
                            rollbackScript);
                        continue;
                    }

                    await shardRetentionRepository.DropShardTableAsync(table.TargetLogicalTable, physicalTable, rollbackScript, ct);
                    deletedCount++;
                }
            }
            catch (Exception ex)
            {
                failedCount++;
                logger.LogError(ex, "分表保留期执行失败。TableCode={TableCode}, TargetLogicalTable={TargetLogicalTable}", table.TableCode, table.TargetLogicalTable);
            }
        }

        if (!hasRetentionTable)
        {
            return "未配置启用保留期治理的表。";
        }

        var summary = $"RetentionCleanup完成。Scanned={scannedCount}, Deleted={deletedCount}, DryRun={dryRunCount}, Failed={failedCount}";
        logger.LogInformation(summary);
        return summary;
    }
}
