using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 分表保留期执行服务实现。
/// </summary>
public class RetentionExecutionService(
    ISyncTaskConfigRepository taskConfigRepository,
    IShardTableResolver shardTableResolver,
    IShardRetentionRepository shardRetentionRepository,
    IOptions<RetentionJobOptions> retentionJobOptions,
    ILogger<RetentionExecutionService> logger) : IRetentionExecutionService
{
    /// <summary>SQL 标识符安全校验正则（仅允许字母、数字、下划线）。</summary>
    private static readonly Regex SqlIdentifierRegex = new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    /// <summary>保留期任务配置快照。</summary>
    private readonly RetentionJobOptions _retentionJobOptions = retentionJobOptions.Value;

    /// <inheritdoc/>
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

    /// <summary>
    /// 构建保留期执行目标集合（同步表 + 日志表）。
    /// </summary>
    /// <param name="enabledTables">已启用同步表定义。</param>
    /// <returns>保留期执行目标集合。</returns>
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

        foreach (var logTable in _retentionJobOptions.LogTables)
        {
            if (!logTable.Enabled)
            {
                continue;
            }

            var logicalTableName = logTable.LogicalTableName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(logicalTableName) || !IsSafeSqlIdentifier(logicalTableName))
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
    /// 校验 SQL 标识符是否合法。
    /// </summary>
    /// <param name="identifier">待校验标识符。</param>
    /// <returns>合法返回 <c>true</c>。</returns>
    private static bool IsSafeSqlIdentifier(string identifier)
    {
        return !string.IsNullOrWhiteSpace(identifier) && SqlIdentifierRegex.IsMatch(identifier);
    }

    /// <summary>
    /// 保留期执行目标。
    /// </summary>
    /// <param name="TargetCode">目标编码。</param>
    /// <param name="LogicalTableName">逻辑表名。</param>
    /// <param name="KeepMonths">保留月数。</param>
    /// <param name="DryRun">是否 dry-run。</param>
    /// <param name="AllowDrop">是否允许删除。</param>
    private readonly record struct RetentionTarget(
        string TargetCode,
        string LogicalTableName,
        int KeepMonths,
        bool DryRun,
        bool AllowDrop);
}
