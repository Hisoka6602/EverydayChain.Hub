using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Domain.Aggregates.DashboardSnapshotAggregate;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 定义 DashboardSnapshotService 类型。
/// </summary>
public sealed class DashboardSnapshotService : IDashboardSnapshotService
{
    /// <summary>
    /// 存储 BusinessTaskLogicalTable 字段。
    /// </summary>
    private const string BusinessTaskLogicalTable = "business_tasks";
    /// <summary>
    /// 存储 ScanLogLogicalTable 字段。
    /// </summary>
    private const string ScanLogLogicalTable = "scan_logs";
    /// <summary>
    /// 存储 LeaseKey 字段。
    /// </summary>
    private const string LeaseKey = "dashboard-snapshot-refresh";
    /// <summary>
    /// 存储 EmptyWaveCode 字段。
    /// </summary>
    private const string EmptyWaveCode = "未分波次";

    /// <summary>
    /// 存储 _contextFactory 字段。
    /// </summary>
    private readonly IDbContextFactory<HubDbContext> _contextFactory;
    /// <summary>
    /// 存储 _shardSuffixResolver 字段。
    /// </summary>
    private readonly IShardSuffixResolver _shardSuffixResolver;
    /// <summary>
    /// 存储 _shardTableResolver 字段。
    /// </summary>
    private readonly IShardTableResolver _shardTableResolver;
    /// <summary>
    /// 存储 _runtimeLeaseRepository 字段。
    /// </summary>
    private readonly IRuntimeLeaseRepository _runtimeLeaseRepository;
    /// <summary>
    /// 存储 _options 字段。
    /// </summary>
    private readonly DashboardSnapshotOptions _options;
    /// <summary>
    /// 存储 _shardingOptions 字段。
    /// </summary>
    private readonly ShardingOptions _shardingOptions;
    /// <summary>
    /// 存储 _logger 字段。
    /// </summary>
    private readonly ILogger<DashboardSnapshotService> _logger;
    /// <summary>
    /// 存储 _ownerId 字段。
    /// </summary>
    private readonly string _ownerId = $"{Environment.MachineName}:{Guid.NewGuid():N}";

    /// <summary>
    /// 执行 DashboardSnapshotService 方法。
    /// </summary>
    public DashboardSnapshotService(
        IDbContextFactory<HubDbContext> contextFactory,
        IShardSuffixResolver shardSuffixResolver,
        IShardTableResolver shardTableResolver,
        IRuntimeLeaseRepository runtimeLeaseRepository,
        IOptions<DashboardSnapshotOptions> options,
        IOptions<ShardingOptions> shardingOptions,
        ILogger<DashboardSnapshotService> logger)
    {
        // 步骤：执行 NewGuid 方法的核心处理流程。
        _contextFactory = contextFactory;
        _shardSuffixResolver = shardSuffixResolver;
        _shardTableResolver = shardTableResolver;
        _runtimeLeaseRepository = runtimeLeaseRepository;
        _options = options.Value;
        _shardingOptions = shardingOptions.Value;
        _logger = logger;
    }

    public async Task RefreshAsync(CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var nowLocal = DateTime.Now;
        var acquired = await _runtimeLeaseRepository.TryAcquireAsync(
            LeaseKey,
            _ownerId,
            nowLocal,
            nowLocal.AddSeconds(Math.Max(30, _options.RefreshLeaseSeconds)),
            ct);
        if (!acquired)
        {
            _logger.LogDebug("Dashboard snapshot refresh skipped because another worker currently owns the lease.");
            return;
        }

        try
        {
            var coverageEndLocal = TruncateToMinute(nowLocal).AddMinutes(1);
            var coverageStartLocal = coverageEndLocal.AddHours(-Math.Max(1, _options.InitialBackfillHours));

            await RefreshTaskSnapshotsAsync(coverageStartLocal, coverageEndLocal, nowLocal, ct);
            await RefreshScanSnapshotsAsync(coverageStartLocal, coverageEndLocal, nowLocal, ct);
        }
        finally
        {
            await _runtimeLeaseRepository.ReleaseAsync(LeaseKey, _ownerId, ct);
        }
    }

    private async Task RefreshTaskSnapshotsAsync(DateTime coverageStartLocal, DateTime coverageEndLocal, DateTime refreshTimeLocal, CancellationToken ct)
    {
        var state = await GetSnapshotStateAsync(DashboardSnapshotSource.BusinessTask, ct);
        var fullRefresh = ShouldRunFullRefresh(state, coverageStartLocal, coverageEndLocal);
        var dirtyBuckets = fullRefresh
            ? EnumerateMinuteBuckets(coverageStartLocal, coverageEndLocal).ToHashSet()
            : await LoadDirtyTaskBucketsAsync(state!, coverageStartLocal, coverageEndLocal, refreshTimeLocal, ct);

        var rows = dirtyBuckets.Count == 0
            ? []
            : await LoadTaskSnapshotRowsAsync(BuildContiguousRanges(dirtyBuckets), ct);
        var currentWaveRows = dirtyBuckets.Count == 0
            ? []
            : await LoadCurrentWaveSnapshotRowsAsync(BuildContiguousRanges(dirtyBuckets), ct);

        await PersistTaskSnapshotsAsync(
            dirtyBuckets,
            rows,
            coverageStartLocal,
            coverageEndLocal,
            refreshTimeLocal,
            ct);
        await PersistCurrentWaveSnapshotsAsync(
            dirtyBuckets,
            currentWaveRows,
            coverageStartLocal,
            coverageEndLocal,
            refreshTimeLocal,
            ct);
    }

    private async Task RefreshScanSnapshotsAsync(DateTime coverageStartLocal, DateTime coverageEndLocal, DateTime refreshTimeLocal, CancellationToken ct)
    {
        var state = await GetSnapshotStateAsync(DashboardSnapshotSource.ScanLog, ct);
        var fullRefresh = ShouldRunFullRefresh(state, coverageStartLocal, coverageEndLocal);
        var dirtyBuckets = fullRefresh
            ? EnumerateMinuteBuckets(coverageStartLocal, coverageEndLocal).ToHashSet()
            : await LoadDirtyScanBucketsAsync(state!, coverageStartLocal, coverageEndLocal, refreshTimeLocal, ct);

        var rows = dirtyBuckets.Count == 0
            ? []
            : await LoadScanSnapshotRowsAsync(BuildContiguousRanges(dirtyBuckets), ct);

        await PersistScanSnapshotsAsync(
            dirtyBuckets,
            rows,
            coverageStartLocal,
            coverageEndLocal,
            refreshTimeLocal,
            ct);
    }

    private bool ShouldRunFullRefresh(DashboardSnapshotStateEntity? state, DateTime coverageStartLocal, DateTime coverageEndLocal)
    {
        if (state is null || state.CoverageStartLocal is null || state.CoverageEndLocal is null || state.LastRefreshTimeLocal is null)
        {
            return true;
        }

        return state.CoverageStartLocal.Value > coverageStartLocal
            || state.CoverageEndLocal.Value < coverageStartLocal
            || state.CoverageEndLocal.Value > coverageEndLocal.AddMinutes(5);
    }

    private async Task<DashboardSnapshotStateEntity?> GetSnapshotStateAsync(DashboardSnapshotSource source, CancellationToken ct)
    {
        using var scope = TableSuffixScope.Use(string.Empty);
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.DashboardSnapshotStates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == source, ct);
    }

    /// <summary>
    /// 执行 LoadDirtyTaskBucketsAsync 方法。
    /// </summary>
    private async Task<HashSet<DateTime>> LoadDirtyTaskBucketsAsync(
        DashboardSnapshotStateEntity state,
        DateTime coverageStartLocal,
        DateTime coverageEndLocal,
        DateTime refreshTimeLocal,
        CancellationToken ct)
    {
        // 步骤：执行 LoadDirtyTaskBucketsAsync 方法的核心处理流程。
        var buckets = new HashSet<DateTime>();
        var lastRefreshTimeLocal = state.LastRefreshTimeLocal ?? coverageStartLocal;
        var changedSinceLocal = lastRefreshTimeLocal.AddSeconds(-Math.Max(0, _options.RefreshOverlapSeconds));

        foreach (var bucket in EnumerateMinuteBuckets(MaxDateTime(state.CoverageEndLocal ?? coverageStartLocal, coverageStartLocal), coverageEndLocal))
        {
            buckets.Add(bucket);
        }

        foreach (var suffix in await ListBusinessTaskSuffixesByCreatedRangeAsync(coverageStartLocal, coverageEndLocal, ct))
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await _contextFactory.CreateDbContextAsync(ct);
            var minuteOffsets = await db.BusinessTasks
                .AsNoTracking()
                .Where(x => x.CreatedTimeLocal >= coverageStartLocal && x.CreatedTimeLocal < coverageEndLocal)
                .Where(x => x.UpdatedTimeLocal >= changedSinceLocal && x.UpdatedTimeLocal < refreshTimeLocal)
                .GroupBy(x => EF.Functions.DateDiffMinute(coverageStartLocal, x.CreatedTimeLocal))
                .Select(group => group.Key)
                .ToListAsync(ct);
            foreach (var minuteOffset in minuteOffsets)
            {
                buckets.Add(coverageStartLocal.AddMinutes(minuteOffset));
            }
        }

        return buckets;
    }

    /// <summary>
    /// 执行 LoadDirtyScanBucketsAsync 方法。
    /// </summary>
    private async Task<HashSet<DateTime>> LoadDirtyScanBucketsAsync(
        DashboardSnapshotStateEntity state,
        DateTime coverageStartLocal,
        DateTime coverageEndLocal,
        DateTime refreshTimeLocal,
        CancellationToken ct)
    {
        // 步骤：执行 LoadDirtyScanBucketsAsync 方法的核心处理流程。
        var buckets = new HashSet<DateTime>();
        var lastRefreshTimeLocal = state.LastRefreshTimeLocal ?? coverageStartLocal;
        var changedSinceLocal = lastRefreshTimeLocal.AddSeconds(-Math.Max(0, _options.RefreshOverlapSeconds));

        foreach (var bucket in EnumerateMinuteBuckets(MaxDateTime(state.CoverageEndLocal ?? coverageStartLocal, coverageStartLocal), coverageEndLocal))
        {
            buckets.Add(bucket);
        }

        foreach (var suffix in await ListScanLogSuffixesByScanRangeAsync(coverageStartLocal, coverageEndLocal, ct))
        {
            using var scope = TableSuffixScope.Use(suffix);
            await using var db = await _contextFactory.CreateDbContextAsync(ct);
            var minuteOffsets = await db.ScanLogs
                .AsNoTracking()
                .Where(x => x.ScanTimeLocal >= coverageStartLocal && x.ScanTimeLocal < coverageEndLocal)
                .Where(x => x.CreatedTimeLocal >= changedSinceLocal && x.CreatedTimeLocal < refreshTimeLocal)
                .GroupBy(x => EF.Functions.DateDiffMinute(coverageStartLocal, x.ScanTimeLocal))
                .Select(group => group.Key)
                .ToListAsync(ct);
            foreach (var minuteOffset in minuteOffsets)
            {
                buckets.Add(coverageStartLocal.AddMinutes(minuteOffset));
            }
        }

        return buckets;
    }

    /// <summary>
    /// 执行 LoadTaskSnapshotRowsAsync 方法。
    /// </summary>
    private async Task<List<DashboardTaskSnapshotEntity>> LoadTaskSnapshotRowsAsync(
        IReadOnlyList<SnapshotBucketRange> ranges,
        CancellationToken ct)
    {
        // 步骤：执行 LoadTaskSnapshotRowsAsync 方法的核心处理流程。
        var rows = new List<DashboardTaskSnapshotEntity>();
        foreach (var range in ranges)
        {
            foreach (var suffix in await ListBusinessTaskSuffixesByCreatedRangeAsync(range.StartLocal, range.EndLocal, ct))
            {
                using var scope = TableSuffixScope.Use(suffix);
                await using var db = await _contextFactory.CreateDbContextAsync(ct);
                var shardRows = await db.BusinessTasks
                    .AsNoTracking()
                    .Where(x => x.CreatedTimeLocal >= range.StartLocal && x.CreatedTimeLocal < range.EndLocal)
                    .GroupBy(x => new
                    {
                        MinuteOffset = EF.Functions.DateDiffMinute(range.StartLocal, x.CreatedTimeLocal),
                        WaveCode = x.NormalizedWaveCode ?? EmptyWaveCode,
                        x.WaveRemark,
                        x.ResolvedDockCode,
                        x.WorkingArea,
                        x.SourceType,
                        x.Status
                    })
                    .Select(group => new TaskSnapshotRow
                    {
                        MinuteOffset = group.Key.MinuteOffset,
                        WaveCode = group.Key.WaveCode,
                        WaveRemark = group.Key.WaveRemark,
                        ResolvedDockCode = group.Key.ResolvedDockCode,
                        WorkingArea = group.Key.WorkingArea,
                        SourceType = group.Key.SourceType,
                        Status = group.Key.Status,
                        TotalCount = group.Count(),
                        ScannedCount = group.Count(x => x.ScannedAtLocal != null),
                        RecirculatedCount = group.Count(x =>
                            x.ResolvedDockCode != string.Empty
                            && !EF.Functions.Like(x.ResolvedDockCode, "%[^0-9]%")
                            && Convert.ToInt32(x.ResolvedDockCode) > 7),
                        ExceptionCount = group.Count(x => x.IsException || x.Status == BusinessTaskStatus.Exception),
                        RequiredFeedbackCount = group.Count(x => x.FeedbackStatus != BusinessTaskFeedbackStatus.NotRequired),
                        CompletedFeedbackCount = group.Count(x => x.FeedbackStatus == BusinessTaskFeedbackStatus.Completed),
                        TotalVolumeMm3 = group.Sum(x => x.VolumeMm3 ?? 0M),
                        TotalWeightGram = group.Sum(x => x.WeightGram ?? 0M),
                        EarliestCreatedTimeLocal = group.Min(x => x.CreatedTimeLocal),
                        LatestUpdatedTimeLocal = group.Max(x => x.UpdatedTimeLocal)
                    })
                    .ToListAsync(ct);

                rows.AddRange(shardRows.Select(row => new DashboardTaskSnapshotEntity
                {
                    BucketStartLocal = range.StartLocal.AddMinutes(row.MinuteOffset),
                    WaveCode = row.WaveCode,
                    WaveRemark = NormalizeOptionalText(row.WaveRemark),
                    ResolvedDockCode = row.ResolvedDockCode,
                    WorkingArea = NormalizeOptionalText(row.WorkingArea),
                    SourceType = row.SourceType,
                    Status = row.Status,
                    TotalCount = row.TotalCount,
                    ScannedCount = row.ScannedCount,
                    RecirculatedCount = row.RecirculatedCount,
                    ExceptionCount = row.ExceptionCount,
                    RequiredFeedbackCount = row.RequiredFeedbackCount,
                    CompletedFeedbackCount = row.CompletedFeedbackCount,
                    TotalVolumeMm3 = row.TotalVolumeMm3,
                    TotalWeightGram = row.TotalWeightGram,
                    EarliestCreatedTimeLocal = row.EarliestCreatedTimeLocal,
                    LatestUpdatedTimeLocal = row.LatestUpdatedTimeLocal
                }));
            }
        }

        return rows;
    }

    /// <summary>
    /// 执行 LoadScanSnapshotRowsAsync 方法。
    /// </summary>
    private async Task<List<DashboardScanSnapshotEntity>> LoadScanSnapshotRowsAsync(
        IReadOnlyList<SnapshotBucketRange> ranges,
        CancellationToken ct)
    {
        // 步骤：执行 LoadScanSnapshotRowsAsync 方法的核心处理流程。
        var rows = new List<DashboardScanSnapshotEntity>();
        foreach (var range in ranges)
        {
            foreach (var suffix in await ListScanLogSuffixesByScanRangeAsync(range.StartLocal, range.EndLocal, ct))
            {
                using var scope = TableSuffixScope.Use(suffix);
                await using var db = await _contextFactory.CreateDbContextAsync(ct);
                var shardRows = await db.ScanLogs
                    .AsNoTracking()
                    .Where(x => x.ScanTimeLocal >= range.StartLocal && x.ScanTimeLocal < range.EndLocal)
                    .GroupBy(x => EF.Functions.DateDiffMinute(range.StartLocal, x.ScanTimeLocal))
                    .Select(group => new ScanSnapshotRow
                    {
                        MinuteOffset = group.Key,
                        TotalScanCount = group.Count(),
                        MatchedScanCount = group.Count(x => x.IsMatched)
                    })
                    .ToListAsync(ct);

                rows.AddRange(shardRows.Select(row => new DashboardScanSnapshotEntity
                {
                    BucketStartLocal = range.StartLocal.AddMinutes(row.MinuteOffset),
                    TotalScanCount = row.TotalScanCount,
                    MatchedScanCount = row.MatchedScanCount
                }));
            }
        }

        return rows;
    }

    /// <summary>
    /// 读取当前波次分钟快照行。
    /// </summary>
    /// <param name="ranges">待刷新时间段集合。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>当前波次分钟快照行集合。</returns>
    private async Task<List<DashboardCurrentWaveSnapshotEntity>> LoadCurrentWaveSnapshotRowsAsync(
        IReadOnlyList<SnapshotBucketRange> ranges,
        CancellationToken ct)
    {
        // 步骤：执行 LoadCurrentWaveSnapshotRowsAsync 方法的核心处理流程。
        var rows = new List<DashboardCurrentWaveSnapshotEntity>();
        foreach (var range in ranges)
        {
            foreach (var suffix in await ListBusinessTaskSuffixesByCreatedRangeAsync(range.StartLocal, range.EndLocal, ct))
            {
                using var scope = TableSuffixScope.Use(suffix);
                await using var db = await _contextFactory.CreateDbContextAsync(ct);
                var shardRows = await db.BusinessTasks
                    .AsNoTracking()
                    .Where(x => x.CreatedTimeLocal >= range.StartLocal && x.CreatedTimeLocal < range.EndLocal)
                    .Where(x => x.ScannedAtLocal != null && x.NormalizedWaveCode != null)
                    .GroupBy(x => EF.Functions.DateDiffMinute(range.StartLocal, x.CreatedTimeLocal))
                    .Select(group => group
                        .OrderByDescending(x => x.ScannedAtLocal)
                        .ThenByDescending(x => x.UpdatedTimeLocal)
                        .ThenByDescending(x => x.Id)
                        .Select(x => new CurrentWaveSnapshotRow
                        {
                            MinuteOffset = group.Key,
                            ScannedAtLocal = x.ScannedAtLocal!.Value,
                            WaveCode = x.NormalizedWaveCode!,
                            WaveRemark = x.WaveRemark,
                            Barcode = x.Barcode ?? string.Empty
                        })
                        .FirstOrDefault())
                    .Where(x => x != null)
                    .ToListAsync(ct);

                rows.AddRange(shardRows
                    .Where(row => row is not null)
                    .Select(row => new DashboardCurrentWaveSnapshotEntity
                    {
                        BucketStartLocal = range.StartLocal.AddMinutes(row!.MinuteOffset),
                        ScannedAtLocal = row.ScannedAtLocal,
                        WaveCode = row.WaveCode,
                        WaveRemark = NormalizeOptionalText(row.WaveRemark),
                        Barcode = row.Barcode
                    }));
            }
        }

        return rows;
    }

    /// <summary>
    /// 执行 PersistTaskSnapshotsAsync 方法。
    /// </summary>
    private async Task PersistTaskSnapshotsAsync(
        HashSet<DateTime> dirtyBuckets,
        IReadOnlyList<DashboardTaskSnapshotEntity> rows,
        DateTime coverageStartLocal,
        DateTime coverageEndLocal,
        DateTime refreshTimeLocal,
        CancellationToken ct)
    {
        // 步骤：执行 NormalizeOptionalText 方法的核心处理流程。
        using var scope = TableSuffixScope.Use(string.Empty);
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await db.DashboardTaskSnapshots
            .Where(x => x.BucketStartLocal < coverageStartLocal || x.BucketStartLocal >= coverageEndLocal)
            .ExecuteDeleteAsync(ct);

        foreach (var range in BuildContiguousRanges(dirtyBuckets))
        {
            await db.DashboardTaskSnapshots
                .Where(x => x.BucketStartLocal >= range.StartLocal && x.BucketStartLocal < range.EndLocal)
                .ExecuteDeleteAsync(ct);
        }

        if (rows.Count > 0)
        {
            db.DashboardTaskSnapshots.AddRange(rows);
        }

        await UpsertSnapshotStateAsync(
            db,
            DashboardSnapshotSource.BusinessTask,
            coverageStartLocal,
            coverageEndLocal,
            refreshTimeLocal,
            ct);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    /// <summary>
    /// 执行 PersistScanSnapshotsAsync 方法。
    /// </summary>
    private async Task PersistScanSnapshotsAsync(
        HashSet<DateTime> dirtyBuckets,
        IReadOnlyList<DashboardScanSnapshotEntity> rows,
        DateTime coverageStartLocal,
        DateTime coverageEndLocal,
        DateTime refreshTimeLocal,
        CancellationToken ct)
    {
        // 步骤：执行 AddRange 方法的核心处理流程。
        using var scope = TableSuffixScope.Use(string.Empty);
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await db.DashboardScanSnapshots
            .Where(x => x.BucketStartLocal < coverageStartLocal || x.BucketStartLocal >= coverageEndLocal)
            .ExecuteDeleteAsync(ct);

        foreach (var range in BuildContiguousRanges(dirtyBuckets))
        {
            await db.DashboardScanSnapshots
                .Where(x => x.BucketStartLocal >= range.StartLocal && x.BucketStartLocal < range.EndLocal)
                .ExecuteDeleteAsync(ct);
        }

        if (rows.Count > 0)
        {
            db.DashboardScanSnapshots.AddRange(rows);
        }

        await UpsertSnapshotStateAsync(
            db,
            DashboardSnapshotSource.ScanLog,
            coverageStartLocal,
            coverageEndLocal,
            refreshTimeLocal,
            ct);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    /// <summary>
    /// 持久化当前波次分钟快照。
    /// </summary>
    /// <param name="dirtyBuckets">脏桶集合。</param>
    /// <param name="rows">当前波次快照行集合。</param>
    /// <param name="coverageStartLocal">覆盖开始时间。</param>
    /// <param name="coverageEndLocal">覆盖结束时间。</param>
    /// <param name="refreshTimeLocal">刷新时间。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task PersistCurrentWaveSnapshotsAsync(
        HashSet<DateTime> dirtyBuckets,
        IReadOnlyList<DashboardCurrentWaveSnapshotEntity> rows,
        DateTime coverageStartLocal,
        DateTime coverageEndLocal,
        DateTime refreshTimeLocal,
        CancellationToken ct)
    {
        // 步骤：执行 AddRange 方法的核心处理流程。
        using var scope = TableSuffixScope.Use(string.Empty);
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await db.DashboardCurrentWaveSnapshots
            .Where(x => x.BucketStartLocal < coverageStartLocal || x.BucketStartLocal >= coverageEndLocal)
            .ExecuteDeleteAsync(ct);

        foreach (var range in BuildContiguousRanges(dirtyBuckets))
        {
            await db.DashboardCurrentWaveSnapshots
                .Where(x => x.BucketStartLocal >= range.StartLocal && x.BucketStartLocal < range.EndLocal)
                .ExecuteDeleteAsync(ct);
        }

        if (rows.Count > 0)
        {
            db.DashboardCurrentWaveSnapshots.AddRange(rows);
        }

        await UpsertSnapshotStateAsync(
            db,
            DashboardSnapshotSource.CurrentWave,
            coverageStartLocal,
            coverageEndLocal,
            refreshTimeLocal,
            ct);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    /// <summary>
    /// 执行 UpsertSnapshotStateAsync 方法。
    /// </summary>
    private static async Task UpsertSnapshotStateAsync(
        HubDbContext db,
        DashboardSnapshotSource source,
        DateTime coverageStartLocal,
        DateTime coverageEndLocal,
        DateTime refreshTimeLocal,
        CancellationToken ct)
    {
        // 步骤：执行 AddRange 方法的核心处理流程。
        var state = await db.DashboardSnapshotStates.FirstOrDefaultAsync(x => x.Id == source, ct);
        if (state is null)
        {
            state = new DashboardSnapshotStateEntity
            {
                Id = source
            };
            db.DashboardSnapshotStates.Add(state);
        }

        state.CoverageStartLocal = coverageStartLocal;
        state.CoverageEndLocal = coverageEndLocal;
        state.LastRefreshTimeLocal = refreshTimeLocal;
    }

    /// <summary>
    /// 执行 ListBusinessTaskSuffixesByCreatedRangeAsync 方法。
    /// </summary>
    private async Task<IReadOnlyList<string>> ListBusinessTaskSuffixesByCreatedRangeAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        CancellationToken ct)
    {
        // 步骤：执行 ListBusinessTaskSuffixesByCreatedRangeAsync 方法的核心处理流程。
        var availableSuffixes = await ListBusinessTaskSuffixesAsync(ct);
        if (availableSuffixes.Count == 0)
        {
            return [];
        }

        var targetSuffixes = new HashSet<string>(StringComparer.Ordinal);
        var currentMonth = new DateTime(startTimeLocal.Year, startTimeLocal.Month, 1, 0, 0, 0);
        var endBoundaryMonth = new DateTime(endTimeLocal.AddTicks(-1).Year, endTimeLocal.AddTicks(-1).Month, 1, 0, 0, 0);
        while (currentMonth <= endBoundaryMonth)
        {
            targetSuffixes.Add(_shardSuffixResolver.ResolveLocal(currentMonth));
            currentMonth = currentMonth.AddMonths(1);
        }

        return availableSuffixes
            .Where(suffix => targetSuffixes.Contains(suffix) || (_shardingOptions.EnableLegacyBaseTableReadFallback && string.IsNullOrEmpty(suffix)))
            .ToList();
    }

    private async Task<IReadOnlyList<string>> ListBusinessTaskSuffixesAsync(CancellationToken ct)
    {
        var tables = await _shardTableResolver.ListPhysicalTablesAsync(BusinessTaskLogicalTable, ct);
        var suffixes = tables
            .Select(table => table[BusinessTaskLogicalTable.Length..])
            .Where(suffix => !string.IsNullOrWhiteSpace(suffix))
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(suffix => suffix, StringComparer.Ordinal)
            .ToList();
        if (_shardingOptions.EnableLegacyBaseTableReadFallback)
        {
            suffixes.Add(string.Empty);
        }

        return suffixes;
    }

    /// <summary>
    /// 执行 ListScanLogSuffixesByScanRangeAsync 方法。
    /// </summary>
    private async Task<IReadOnlyList<string>> ListScanLogSuffixesByScanRangeAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        CancellationToken ct)
    {
        // 步骤：执行 ListScanLogSuffixesByScanRangeAsync 方法的核心处理流程。
        var tables = await _shardTableResolver.ListPhysicalTablesAsync(ScanLogLogicalTable, ct);
        var availableSuffixes = tables
            .Select(table => table[ScanLogLogicalTable.Length..])
            .Where(suffix => !string.IsNullOrWhiteSpace(suffix))
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(suffix => suffix, StringComparer.Ordinal)
            .ToList();

        var targetSuffixes = new HashSet<string>(StringComparer.Ordinal);
        var currentMonth = new DateTime(startTimeLocal.Year, startTimeLocal.Month, 1, 0, 0, 0);
        var endBoundaryMonth = new DateTime(endTimeLocal.AddTicks(-1).Year, endTimeLocal.AddTicks(-1).Month, 1, 0, 0, 0);
        while (currentMonth <= endBoundaryMonth)
        {
            targetSuffixes.Add(_shardSuffixResolver.ResolveLocal(currentMonth));
            currentMonth = currentMonth.AddMonths(1);
        }

        return availableSuffixes.Where(targetSuffixes.Contains).ToList();
    }

    private static IReadOnlyList<SnapshotBucketRange> BuildContiguousRanges(IEnumerable<DateTime> buckets)
    {
        var ordered = buckets
            .Distinct()
            .OrderBy(x => x)
            .ToList();
        if (ordered.Count == 0)
        {
            return [];
        }

        var ranges = new List<SnapshotBucketRange>();
        var currentStart = ordered[0];
        var currentEnd = currentStart.AddMinutes(1);
        for (var index = 1; index < ordered.Count; index++)
        {
            var bucket = ordered[index];
            if (bucket == currentEnd)
            {
                currentEnd = currentEnd.AddMinutes(1);
                continue;
            }

            ranges.Add(new SnapshotBucketRange(currentStart, currentEnd));
            currentStart = bucket;
            currentEnd = bucket.AddMinutes(1);
        }

        ranges.Add(new SnapshotBucketRange(currentStart, currentEnd));
        return ranges;
    }

    private static IEnumerable<DateTime> EnumerateMinuteBuckets(DateTime startLocal, DateTime endLocal)
    {
        for (var current = startLocal; current < endLocal; current = current.AddMinutes(1))
        {
            yield return current;
        }
    }

    private static DateTime TruncateToMinute(DateTime value)
    {
        return new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Kind);
    }

    private static DateTime MaxDateTime(DateTime left, DateTime right)
    {
        return left >= right ? left : right;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// 定义 TaskSnapshotRow 类型。
    /// </summary>
    private sealed class TaskSnapshotRow
    {
        /// <summary>
        /// 获取或设置 MinuteOffset。
        /// </summary>
        public int MinuteOffset { get; set; }

        /// <summary>
        /// 获取或设置 WaveCode。
        /// </summary>
        public string WaveCode { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置 WaveRemark。
        /// </summary>
        public string? WaveRemark { get; set; }

        /// <summary>
        /// 获取或设置 ResolvedDockCode。
        /// </summary>
        public string ResolvedDockCode { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置 WorkingArea。
        /// </summary>
        public string? WorkingArea { get; set; }

        /// <summary>
        /// 获取或设置 SourceType。
        /// </summary>
        public BusinessTaskSourceType SourceType { get; set; }

        /// <summary>
        /// 获取或设置 Status。
        /// </summary>
        public BusinessTaskStatus Status { get; set; }

        /// <summary>
        /// 获取或设置 TotalCount。
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 获取或设置 ScannedCount。
        /// </summary>
        public int ScannedCount { get; set; }

        /// <summary>
        /// 获取或设置 RecirculatedCount。
        /// </summary>
        public int RecirculatedCount { get; set; }

        /// <summary>
        /// 获取或设置 ExceptionCount。
        /// </summary>
        public int ExceptionCount { get; set; }

        /// <summary>
        /// 获取或设置 RequiredFeedbackCount。
        /// </summary>
        public int RequiredFeedbackCount { get; set; }

        /// <summary>
        /// 获取或设置 CompletedFeedbackCount。
        /// </summary>
        public int CompletedFeedbackCount { get; set; }

        /// <summary>
        /// 获取或设置 TotalVolumeMm3。
        /// </summary>
        public decimal TotalVolumeMm3 { get; set; }

        /// <summary>
        /// 获取或设置 TotalWeightGram。
        /// </summary>
        public decimal TotalWeightGram { get; set; }

        /// <summary>
        /// 获取或设置 EarliestCreatedTimeLocal。
        /// </summary>
        public DateTime EarliestCreatedTimeLocal { get; set; }

        /// <summary>
        /// 获取或设置 LatestUpdatedTimeLocal。
        /// </summary>
        public DateTime LatestUpdatedTimeLocal { get; set; }
    }

    /// <summary>
    /// 定义 ScanSnapshotRow 类型。
    /// </summary>
    private sealed class ScanSnapshotRow
    {
        /// <summary>
        /// 获取或设置 MinuteOffset。
        /// </summary>
        public int MinuteOffset { get; set; }

        /// <summary>
        /// 获取或设置 TotalScanCount。
        /// </summary>
        public int TotalScanCount { get; set; }

        /// <summary>
        /// 获取或设置 MatchedScanCount。
        /// </summary>
        public int MatchedScanCount { get; set; }
    }

    /// <summary>
    /// 定义 CurrentWaveSnapshotRow 类型。
    /// </summary>
    private sealed class CurrentWaveSnapshotRow
    {
        /// <summary>
        /// 获取或设置 MinuteOffset。
        /// </summary>
        public int MinuteOffset { get; set; }

        /// <summary>
        /// 获取或设置 ScannedAtLocal。
        /// </summary>
        public DateTime ScannedAtLocal { get; set; }

        /// <summary>
        /// 获取或设置 WaveCode。
        /// </summary>
        public string WaveCode { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置 WaveRemark。
        /// </summary>
        public string? WaveRemark { get; set; }

        /// <summary>
        /// 获取或设置 Barcode。
        /// </summary>
        public string Barcode { get; set; } = string.Empty;
    }

    /// <summary>
    /// 定义 SnapshotBucketRange 类型。
    /// </summary>
    private readonly record struct SnapshotBucketRange(DateTime StartLocal, DateTime EndLocal);
}


