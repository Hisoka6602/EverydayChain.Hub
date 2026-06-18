using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 扫描日志仓储 EF Core 实现，按月写入 <c>scan_logs_{yyyyMM}</c> 分表。
/// </summary>
public class ScanLogRepository(
    IDbContextFactory<HubDbContext> contextFactory,
    IShardSuffixResolver shardSuffixResolver,
    IShardTableProvisioner shardTableProvisioner,
    IShardTableResolver shardTableResolver) : IScanLogRepository
{
    /// <summary>扫描日志逻辑表名。</summary>
    private const string ScanLogLogicalTable = "scan_logs";

    /// <inheritdoc/>
    public async Task SaveAsync(ScanLogEntity entity, CancellationToken ct)
    {
        var suffix = shardSuffixResolver.ResolveLocal(entity.ScanTimeLocal);
        await shardTableProvisioner.EnsureShardTableAsync(ScanLogLogicalTable, suffix, ct);
        using var scope = TableSuffixScope.Use(suffix);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        db.ScanLogs.Add(entity);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<ScanLogRecognitionAggregate> AggregateRecognitionAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct)
    {
        if (endTimeLocal <= startTimeLocal)
        {
            return new ScanLogRecognitionAggregate();
        }

        var tables = await shardTableResolver.ListPhysicalTablesAsync(ScanLogLogicalTable, ct);
        var aggregate = new ScanLogRecognitionAggregate();
        foreach (var table in tables)
        {
            ct.ThrowIfCancellationRequested();
            var suffix = table[ScanLogLogicalTable.Length..];
            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            var shardAggregate = await db.ScanLogs
                .AsNoTracking()
                .Where(x => x.ScanTimeLocal >= startTimeLocal && x.ScanTimeLocal < endTimeLocal)
                .GroupBy(_ => 1)
                .Select(group => new ScanLogRecognitionAggregate
                {
                    TotalScanCount = group.Count(),
                    MatchedScanCount = group.Count(x => x.IsMatched)
                })
                .FirstOrDefaultAsync(ct);

            if (shardAggregate is null)
            {
                continue;
            }

            aggregate.TotalScanCount += shardAggregate.TotalScanCount;
            aggregate.MatchedScanCount += shardAggregate.MatchedScanCount;
        }

        return aggregate;
    }

    public async Task<(int TotalCount, IReadOnlyList<ScanLogEntity> Items)> QueryPageAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? barcode,
        string? deviceCode,
        int skip,
        int take,
        CancellationToken ct)
    {
        if (endTimeLocal <= startTimeLocal || take <= 0)
        {
            return (0, Array.Empty<ScanLogEntity>());
        }

        var normalizedBarcode = NormalizeOptionalText(barcode);
        var normalizedDeviceCode = NormalizeOptionalText(deviceCode);
        var rows = new List<ScanLogEntity>();
        foreach (var suffix in await ListShardSuffixesByScanTimeRangeAsync(startTimeLocal, endTimeLocal, ct))
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            var query = db.ScanLogs
                .AsNoTracking()
                .Where(x => x.ScanTimeLocal >= startTimeLocal && x.ScanTimeLocal < endTimeLocal);
            if (!string.IsNullOrWhiteSpace(normalizedBarcode))
            {
                query = query.Where(x => x.Barcode == normalizedBarcode);
            }

            if (!string.IsNullOrWhiteSpace(normalizedDeviceCode))
            {
                query = query.Where(x => x.DeviceCode == normalizedDeviceCode);
            }

            rows.AddRange(await query.ToListAsync(ct));
        }

        var ordered = rows
            .OrderByDescending(x => x.ScanTimeLocal)
            .ThenByDescending(x => x.Id)
            .ToList();

        return (ordered.Count, ordered.Skip(skip).Take(take).ToList());
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private async Task<IReadOnlyList<string>> ListShardSuffixesByScanTimeRangeAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        CancellationToken ct)
    {
        var tables = await shardTableResolver.ListPhysicalTablesAsync(ScanLogLogicalTable, ct);
        var availableSuffixes = tables
            .Select(table => table[ScanLogLogicalTable.Length..])
            .Where(suffix => !string.IsNullOrWhiteSpace(suffix))
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(suffix => suffix, StringComparer.Ordinal)
            .ToList();

        var targetSuffixes = new HashSet<string>(StringComparer.Ordinal);
        var currentMonth = new DateTime(startTimeLocal.Year, startTimeLocal.Month, 1, 0, 0, 0);
        var endInclusiveTime = endTimeLocal.AddTicks(-1);
        var endBoundaryMonth = new DateTime(endInclusiveTime.Year, endInclusiveTime.Month, 1, 0, 0, 0);
        while (currentMonth <= endBoundaryMonth)
        {
            targetSuffixes.Add(shardSuffixResolver.ResolveLocal(currentMonth));
            currentMonth = currentMonth.AddMonths(1);
        }

        return availableSuffixes.Where(targetSuffixes.Contains).ToList();
    }
}
