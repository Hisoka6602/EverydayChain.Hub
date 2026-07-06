using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 定义当前类型。
/// </summary>
public class ScanLogRepository(
    IDbContextFactory<HubDbContext> contextFactory,
    IShardSuffixResolver shardSuffixResolver,
    IShardTableProvisioner shardTableProvisioner,
    IShardTableResolver shardTableResolver,
    IOptions<DashboardSnapshotOptions> dashboardSnapshotOptions) : IScanLogRepository
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string ScanLogLogicalTable = "scan_logs";
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly DashboardSnapshotOptions _dashboardSnapshotOptions = dashboardSnapshotOptions.Value;

    public async Task SaveAsync(ScanLogEntity entity, CancellationToken ct)
    {
        var suffix = shardSuffixResolver.ResolveLocal(entity.ScanTimeLocal);
        await shardTableProvisioner.EnsureShardTableAsync(ScanLogLogicalTable, suffix, ct);
        using var scope = TableSuffixScope.Use(suffix);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        db.ScanLogs.Add(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task<ScanLogRecognitionAggregate> AggregateRecognitionAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct)
    {
        if (endTimeLocal <= startTimeLocal)
        {
            return new ScanLogRecognitionAggregate();
        }

        if (await IsAlignedSnapshotRangeCoveredAsync(startTimeLocal, endTimeLocal, ct))
        {
            using var snapshotScope = TableSuffixScope.Use(string.Empty);
            await using var snapshotDb = await contextFactory.CreateDbContextAsync(ct);
            var snapshotAggregate = await snapshotDb.DashboardScanSnapshots
                .AsNoTracking()
                .Where(x => x.BucketStartLocal >= startTimeLocal && x.BucketStartLocal < endTimeLocal)
                .GroupBy(_ => 1)
                .Select(group => new ScanLogRecognitionAggregate
                {
                    TotalScanCount = group.Sum(x => x.TotalScanCount),
                    MatchedScanCount = group.Sum(x => x.MatchedScanCount)
                })
                .FirstOrDefaultAsync(ct);
            return snapshotAggregate ?? new ScanLogRecognitionAggregate();
        }

        var aggregate = new ScanLogRecognitionAggregate();
        foreach (var suffix in await ListShardSuffixesByScanTimeRangeAsync(startTimeLocal, endTimeLocal, ct))
        {
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

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public async Task<IReadOnlyList<ScanLogEntity>> QueryRangeAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? barcode,
        string? deviceCode,
        CancellationToken ct)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        if (endTimeLocal <= startTimeLocal)
        {
            return Array.Empty<ScanLogEntity>();
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

        return rows
            .OrderByDescending(x => x.ScanTimeLocal)
            .ThenByDescending(x => x.Id)
            .ToList();
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public async Task<(int TotalCount, IReadOnlyList<ScanLogEntity> Items)> QueryPageAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? barcode,
        string? deviceCode,
        int skip,
        int take,
        CancellationToken ct)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        if (take <= 0)
        {
            return (0, Array.Empty<ScanLogEntity>());
        }

        if (endTimeLocal <= startTimeLocal)
        {
            return (0, Array.Empty<ScanLogEntity>());
        }

        var normalizedBarcode = NormalizeOptionalText(barcode);
        var normalizedDeviceCode = NormalizeOptionalText(deviceCode);
        var suffixes = await ListShardSuffixesByScanTimeRangeAsync(startTimeLocal, endTimeLocal, ct);
        var rows = new List<ScanLogEntity>(take);
        var remainingSkip = skip < 0 ? 0 : skip;
        var remainingTake = take;
        var totalCount = 0;

        foreach (var suffix in suffixes)
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            var baseQuery = BuildQueryByFilters(db.ScanLogs.AsNoTracking(), startTimeLocal, endTimeLocal, normalizedBarcode, normalizedDeviceCode);
            var shardCount = await baseQuery.CountAsync(ct);
            if (shardCount <= 0)
            {
                continue;
            }

            totalCount += shardCount;
            if (remainingTake <= 0)
            {
                continue;
            }

            if (remainingSkip >= shardCount)
            {
                remainingSkip -= shardCount;
                continue;
            }

            var shardRows = await baseQuery
                .OrderByDescending(x => x.ScanTimeLocal)
                .ThenByDescending(x => x.Id)
                .Skip(remainingSkip)
                .Take(remainingTake)
                .ToListAsync(ct);
            rows.AddRange(shardRows);
            remainingTake -= shardRows.Count;
            remainingSkip = 0;
        }

        return (totalCount, rows);
    }

    /// <summary>
    /// 按筛选条件构建扫描日志查询。
    /// </summary>
    /// <param name="query">基础查询。</param>
    /// <param name="startTimeLocal">开始时间。</param>
    /// <param name="endTimeLocal">结束时间。</param>
    /// <param name="normalizedBarcode">归一化条码。</param>
    /// <param name="normalizedDeviceCode">归一化设备号。</param>
    /// <returns>筛选后的查询。</returns>
    private static IQueryable<ScanLogEntity> BuildQueryByFilters(
        IQueryable<ScanLogEntity> query,
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? normalizedBarcode,
        string? normalizedDeviceCode)
    {
        // 步骤：先按时间范围筛选，再追加可选条码与设备条件。
        query = query.Where(x => x.ScanTimeLocal >= startTimeLocal && x.ScanTimeLocal < endTimeLocal);
        if (!string.IsNullOrWhiteSpace(normalizedBarcode))
        {
            query = query.Where(x => x.Barcode == normalizedBarcode);
        }

        if (!string.IsNullOrWhiteSpace(normalizedDeviceCode))
        {
            query = query.Where(x => x.DeviceCode == normalizedDeviceCode);
        }

        return query;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private async Task<IReadOnlyList<string>> ListShardSuffixesByScanTimeRangeAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        CancellationToken ct)
    {
        // 步骤：按既定流程执行当前方法逻辑。
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

    private async Task<bool> IsAlignedSnapshotRangeCoveredAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct)
    {
        if (!_dashboardSnapshotOptions.Enabled || !_dashboardSnapshotOptions.PreferSnapshotQueries)
        {
            return false;
        }

        if (!IsMinuteAligned(startTimeLocal) || !IsMinuteAligned(endTimeLocal))
        {
            return false;
        }

        using var scope = TableSuffixScope.Use(string.Empty);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var state = await db.DashboardSnapshotStates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == DashboardSnapshotSource.ScanLog, ct);
        return state?.CoverageStartLocal <= startTimeLocal
            && state.CoverageEndLocal >= endTimeLocal;
    }

    private static bool IsMinuteAligned(DateTime value)
    {
        return value.Second == 0
            && value.Millisecond == 0
            && value.Ticks % TimeSpan.TicksPerSecond == 0;
    }
}

