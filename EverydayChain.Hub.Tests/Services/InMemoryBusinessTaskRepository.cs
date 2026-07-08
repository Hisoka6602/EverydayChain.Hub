using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Queries;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 InMemoryBusinessTaskRepository 类型。
/// </summary>
internal sealed class InMemoryBusinessTaskRepository : IBusinessTaskRepository
{
    /// <summary>
    /// 存储 EmptyWaveCode 字段。
    /// </summary>
    private const string EmptyWaveCode = "未分波次";
    /// <summary>
    /// 存储 EmptyDockCode 字段。
    /// </summary>
    private const string EmptyDockCode = "UNASSIGNED_DOCK";

    private readonly object _gate = new();
    /// <summary>
    /// 存储 _tasks 字段。
    /// </summary>
    private readonly List<BusinessTaskEntity> _tasks = [];
    private readonly BusinessTaskQueryPolicy _queryPolicy = new();
    /// <summary>
    /// 存储 _nextId 字段。
    /// </summary>
    private long _nextId = 1;

    public Task<BusinessTaskEntity?> FindByBarcodeAsync(string barcode, CancellationToken ct)
    {
        lock (_gate)
        {
            return Task.FromResult(_tasks.FirstOrDefault(x => string.Equals(x.Barcode, barcode, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<BusinessTaskEntity?> FindByTaskCodeAsync(string taskCode, CancellationToken ct)
    {
        lock (_gate)
        {
            return Task.FromResult(_tasks.FirstOrDefault(x => string.Equals(x.TaskCode, taskCode, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<BusinessTaskEntity?> FindBySourceTableAndBusinessKeyAsync(string sourceTableCode, string businessKey, CancellationToken ct)
    {
        lock (_gate)
        {
            return Task.FromResult(_tasks.FirstOrDefault(x =>
                string.Equals(x.SourceTableCode, sourceTableCode, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.BusinessKey, businessKey, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<BusinessTaskEntity?> FindByIdAsync(long id, CancellationToken ct)
    {
        lock (_gate)
        {
            return Task.FromResult(_tasks.FirstOrDefault(x => x.Id == id));
        }
    }

    public Task<IReadOnlyDictionary<long, BusinessTaskEntity>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct)
    {
        lock (_gate)
        {
            IReadOnlyDictionary<long, BusinessTaskEntity> result = _tasks
                .Where(task => ids.Contains(task.Id))
                .ToDictionary(task => task.Id);
            return Task.FromResult(result);
        }
    }

    public Task SaveAsync(BusinessTaskEntity entity, CancellationToken ct)
    {
        lock (_gate)
        {
            entity.RefreshQueryFields();
            entity.Id = _nextId++;
            _tasks.Add(entity);
            return Task.CompletedTask;
        }
    }

    public async Task UpsertProjectionAsync(BusinessTaskEntity entity, CancellationToken ct)
    {
        var existing = await FindBySourceTableAndBusinessKeyAsync(entity.SourceTableCode, entity.BusinessKey, ct);
        if (existing is null)
        {
            await SaveAsync(entity, ct);
            return;
        }

        lock (_gate)
        {
            if (existing.Status == BusinessTaskStatus.Created && existing.ScannedAtLocal is null && string.IsNullOrWhiteSpace(existing.Barcode))
            {
                existing.Barcode = entity.Barcode;
            }

            existing.WaveCode = entity.WaveCode;
            existing.WaveRemark = entity.WaveRemark;
            existing.WorkingArea = entity.WorkingArea;
            existing.OrderId = entity.OrderId;
            existing.StoreId = entity.StoreId;
            existing.StoreName = entity.StoreName;
            existing.ProductCode = entity.ProductCode;
            existing.PickLocation = entity.PickLocation;
            existing.UpdatedTimeLocal = entity.UpdatedTimeLocal;
            existing.RefreshQueryFields();
        }
    }

    public async Task<int> UpsertProjectionBatchAsync(IReadOnlyList<BusinessTaskEntity> entities, CancellationToken ct)
    {
        var processedCount = 0;
        foreach (var entity in entities)
        {
            await UpsertProjectionAsync(entity, ct);
            processedCount++;
        }

        return processedCount;
    }

    public Task UpdateAsync(BusinessTaskEntity entity, CancellationToken ct)
    {
        lock (_gate)
        {
            entity.RefreshQueryFields();
            var index = _tasks.FindIndex(x => x.Id == entity.Id);
            if (index >= 0)
            {
                _tasks[index] = entity;
            }

            return Task.CompletedTask;
        }
    }

    public Task<bool> TryMarkScannedAsync(long taskId, DateTime createdTimeLocal, BusinessTaskScanUpdateCommand command, CancellationToken ct)
    {
        lock (_gate)
        {
            var task = _tasks.FirstOrDefault(x => x.Id == taskId && x.CreatedTimeLocal == createdTimeLocal);
            if (task is null || !IsAllowedScanTransitionSourceStatus(task.Status))
            {
                return Task.FromResult(false);
            }

            var wasDropped = task.Status == BusinessTaskStatus.Dropped;
            task.Status = BusinessTaskStatus.Scanned;
            task.DeviceCode = command.DeviceCode;
            task.TraceId = command.TraceId;
            task.Barcode = command.Barcode;
            task.ScannedAtLocal = command.ScanTimeLocal;
            task.UpdatedTimeLocal = command.UpdatedTimeLocal;
            task.LengthMm = command.LengthMm;
            task.WidthMm = command.WidthMm;
            task.HeightMm = command.HeightMm;
            task.VolumeMm3 = command.VolumeMm3;
            task.WeightGram = command.WeightGram;
            task.ScanCount++;

            if (!string.IsNullOrWhiteSpace(command.TargetChuteCode))
            {
                task.TargetChuteCode = command.TargetChuteCode.Trim();
            }

            if (wasDropped)
            {
                task.ActualChuteCode = null;
                task.DroppedAtLocal = null;
                task.FeedbackStatus = BusinessTaskFeedbackStatus.NotRequired;
                task.IsFeedbackReported = false;
                task.FeedbackTimeLocal = null;
            }

            task.IsRecirculated = false;
            task.IsException = false;
            task.FailureReason = null;
            task.RefreshQueryFields();
            return Task.FromResult(true);
        }
    }

    public Task<bool> IncrementScanRetryAsync(long taskId, DateTime createdTimeLocal, DateTime updatedTimeLocal, CancellationToken ct)
    {
        lock (_gate)
        {
            var task = _tasks.FirstOrDefault(x => x.Id == taskId && x.CreatedTimeLocal == createdTimeLocal);
            if (task is null)
            {
                return Task.FromResult(false);
            }

            task.ScanRetryCount++;
            task.UpdatedTimeLocal = updatedTimeLocal;
            return Task.FromResult(true);
        }
    }

    public Task<IReadOnlyList<BusinessTaskEntity>> FindPendingFeedbackAsync(int maxCount, CancellationToken ct)
    {
        lock (_gate)
        {
            IReadOnlyList<BusinessTaskEntity> result = _tasks
                .Where(x => x.FeedbackStatus == BusinessTaskFeedbackStatus.Pending)
                .OrderBy(x => x.CreatedTimeLocal)
                .Take(maxCount)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<IReadOnlyList<BusinessTaskEntity>> FindFailedFeedbackAsync(int maxCount, CancellationToken ct)
    {
        lock (_gate)
        {
            IReadOnlyList<BusinessTaskEntity> result = _tasks
                .Where(x => x.FeedbackStatus == BusinessTaskFeedbackStatus.Failed)
                .OrderBy(x => x.CreatedTimeLocal)
                .Take(maxCount)
                .ToList();
            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// 执行 ClaimFeedbackBatchAsync 方法。
    /// </summary>
    public Task<IReadOnlyList<BusinessTaskEntity>> ClaimFeedbackBatchAsync(
        BusinessTaskFeedbackStatus sourceStatus,
        int maxCount,
        DateTime claimedTimeLocal,
        TimeSpan staleAfter,
        CancellationToken ct)
    {
        // 步骤：执行 ClaimFeedbackBatchAsync 方法的核心处理流程。
        lock (_gate)
        {
            var staleCutoff = claimedTimeLocal - staleAfter;
            var claimed = _tasks
                .Where(task =>
                    task.FeedbackStatus == sourceStatus
                    || (task.FeedbackStatus == BusinessTaskFeedbackStatus.Processing && task.UpdatedTimeLocal <= staleCutoff))
                .OrderBy(task => task.CreatedTimeLocal)
                .ThenBy(task => task.Id)
                .Take(maxCount)
                .ToList();

            foreach (var task in claimed)
            {
                task.FeedbackStatus = BusinessTaskFeedbackStatus.Processing;
                task.UpdatedTimeLocal = claimedTimeLocal;
            }

            return Task.FromResult<IReadOnlyList<BusinessTaskEntity>>(claimed);
        }
    }

    /// <summary>
    /// 执行 ClaimFeedbackByTaskCodeAsync 方法。
    /// </summary>
    public Task<BusinessTaskEntity?> ClaimFeedbackByTaskCodeAsync(
        string taskCode,
        DateTime claimedTimeLocal,
        TimeSpan staleAfter,
        CancellationToken ct)
    {
        // 步骤：执行 ClaimFeedbackByTaskCodeAsync 方法的核心处理流程。
        lock (_gate)
        {
            var staleCutoff = claimedTimeLocal - staleAfter;
            var task = _tasks
                .Where(x => string.Equals(x.TaskCode, taskCode, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault(x =>
                    x.FeedbackStatus == BusinessTaskFeedbackStatus.Failed
                    || (x.FeedbackStatus == BusinessTaskFeedbackStatus.Processing && x.UpdatedTimeLocal <= staleCutoff));
            if (task is null)
            {
                return Task.FromResult<BusinessTaskEntity?>(null);
            }

            task.FeedbackStatus = BusinessTaskFeedbackStatus.Processing;
            task.UpdatedTimeLocal = claimedTimeLocal;
            return Task.FromResult<BusinessTaskEntity?>(task);
        }
    }

    public Task<int> CompleteClaimedFeedbackBatchAsync(IReadOnlyCollection<long> ids, DateTime completedTimeLocal, CancellationToken ct)
    {
        lock (_gate)
        {
            var targets = _tasks
                .Where(task => ids.Contains(task.Id) && task.FeedbackStatus == BusinessTaskFeedbackStatus.Processing)
                .ToList();

            foreach (var task in targets)
            {
                task.FeedbackStatus = BusinessTaskFeedbackStatus.Completed;
                task.IsFeedbackReported = true;
                task.FeedbackTimeLocal = completedTimeLocal;
                task.UpdatedTimeLocal = completedTimeLocal;
            }

            return Task.FromResult(targets.Count);
        }
    }

    public Task<int> FailClaimedFeedbackBatchAsync(IReadOnlyCollection<long> ids, DateTime failedTimeLocal, CancellationToken ct)
    {
        lock (_gate)
        {
            var targets = _tasks
                .Where(task => ids.Contains(task.Id) && task.FeedbackStatus == BusinessTaskFeedbackStatus.Processing)
                .ToList();

            foreach (var task in targets)
            {
                task.FeedbackStatus = BusinessTaskFeedbackStatus.Failed;
                task.IsFeedbackReported = false;
                task.FeedbackTimeLocal = null;
                task.UpdatedTimeLocal = failedTimeLocal;
            }

            return Task.FromResult(targets.Count);
        }
    }

    /// <summary>
    /// 执行 BulkMarkExceptionByWaveCodeAsync 方法。
    /// </summary>
    public Task<int> BulkMarkExceptionByWaveCodeAsync(
        string waveCode,
        BusinessTaskStatus targetStatus,
        string failureReasonPrefix,
        DateTime updatedTimeLocal,
        CancellationToken ct)
    {
        // 步骤：执行 BulkMarkExceptionByWaveCodeAsync 方法的核心处理流程。
        lock (_gate)
        {
            var targets = _tasks
                .Where(x => string.Equals(x.WaveCode, waveCode, StringComparison.OrdinalIgnoreCase)
                    && x.Status != BusinessTaskStatus.Dropped
                    && x.Status != BusinessTaskStatus.Exception)
                .ToList();

            foreach (var task in targets)
            {
                task.Status = targetStatus;
                task.FailureReason = failureReasonPrefix;
                task.UpdatedTimeLocal = updatedTimeLocal;
            }

            return Task.FromResult(targets.Count);
        }
    }

    public Task<IReadOnlyList<BusinessTaskEntity>> FindByWaveCodeAsync(string waveCode, CancellationToken ct)
    {
        lock (_gate)
        {
            IReadOnlyList<BusinessTaskEntity> result = _tasks
                .Where(x => string.Equals(x.WaveCode, waveCode, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.CreatedTimeLocal)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<IReadOnlyList<BusinessTaskEntity>> FindActiveByBarcodeAsync(string barcode, CancellationToken ct)
    {
        lock (_gate)
        {
            IReadOnlyList<BusinessTaskEntity> result = _tasks
                .Where(x => string.Equals(x.Barcode, barcode, StringComparison.OrdinalIgnoreCase)
                    && x.Status != BusinessTaskStatus.Dropped
                    && x.Status != BusinessTaskStatus.Exception)
                .OrderBy(x => x.CreatedTimeLocal)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<IReadOnlyList<BusinessTaskEntity>> FindByCreatedTimeRangeAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct)
    {
        lock (_gate)
        {
            IReadOnlyList<BusinessTaskEntity> result = _tasks
                .Where(x => x.CreatedTimeLocal >= startTimeLocal && x.CreatedTimeLocal < endTimeLocal)
                .OrderBy(x => x.CreatedTimeLocal)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<BusinessTaskEntity?> FindLatestScannedWithWaveByCreatedTimeRangeAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct)
    {
        lock (_gate)
        {
            var result = _tasks
                .Where(task => task.CreatedTimeLocal >= startTimeLocal && task.CreatedTimeLocal < endTimeLocal)
                .Where(task => task.ScannedAtLocal.HasValue && !string.IsNullOrWhiteSpace(task.WaveCode))
                .OrderByDescending(task => task.ScannedAtLocal)
                .ThenByDescending(task => task.UpdatedTimeLocal)
                .ThenByDescending(task => task.Id)
                .FirstOrDefault();
            return Task.FromResult(result);
        }
    }

    public Task<IReadOnlyList<BusinessTaskWaveAggregateRow>> AggregateWaveDashboardAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct)
    {
        lock (_gate)
        {
            IReadOnlyList<BusinessTaskWaveAggregateRow> result = _tasks
                .Where(task => task.CreatedTimeLocal >= startTimeLocal && task.CreatedTimeLocal < endTimeLocal)
                .GroupBy(task => NormalizeWaveCode(task.WaveCode))
                .Select(group => new BusinessTaskWaveAggregateRow
                {
                    WaveCode = group.Key,
                    TotalCount = group.Count(),
                    UnsortedCount = group.Count(task => !IsSorted(task)),
                    FullCaseTotalCount = group.Count(task => task.SourceType == BusinessTaskSourceType.FullCase),
                    FullCaseUnsortedCount = group.Count(task => task.SourceType == BusinessTaskSourceType.FullCase && !IsSorted(task)),
                    SplitTotalCount = group.Count(task => task.SourceType == BusinessTaskSourceType.Split),
                    SplitUnsortedCount = group.Count(task => task.SourceType == BusinessTaskSourceType.Split && !IsSorted(task)),
                    RecognitionCount = group.Count(task => task.ScannedAtLocal.HasValue),
                    RecirculatedCount = group.Count(task => _queryPolicy.IsRecirculatedByResolvedDockCode(task.ResolvedDockCode)),
                    ExceptionCount = group.Count(task => task.IsException || task.Status == BusinessTaskStatus.Exception),
                    TotalVolumeMm3 = group.Sum(task => task.VolumeMm3 ?? 0M),
                    TotalWeightGram = group.Sum(task => task.WeightGram ?? 0M),
                    EarliestCreatedTimeLocal = group.Min(task => task.CreatedTimeLocal)
                })
                .OrderBy(row => row.WaveCode, StringComparer.Ordinal)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<BusinessTaskFeedbackAggregate> AggregateFeedbackAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct)
    {
        lock (_gate)
        {
            var filtered = _tasks
                .Where(task => task.CreatedTimeLocal >= startTimeLocal && task.CreatedTimeLocal < endTimeLocal)
                .ToList();
            return Task.FromResult(new BusinessTaskFeedbackAggregate
            {
                RequiredFeedbackCount = filtered.Count(task => task.FeedbackStatus != BusinessTaskFeedbackStatus.NotRequired),
                CompletedFeedbackCount = filtered.Count(task => task.FeedbackStatus == BusinessTaskFeedbackStatus.Completed)
            });
        }
    }

    public Task<IReadOnlyList<string>> ListWaveCodesByCreatedTimeRangeAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct)
    {
        lock (_gate)
        {
            IReadOnlyList<string> result = _tasks
                .Where(task => task.CreatedTimeLocal >= startTimeLocal && task.CreatedTimeLocal < endTimeLocal)
                .Select(task => NormalizeWaveCode(task.WaveCode))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(code => code, StringComparer.Ordinal)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<IReadOnlyList<BusinessTaskWaveOptionRow>> ListWaveOptionsByCreatedTimeRangeAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct)
    {
        lock (_gate)
        {
            IReadOnlyList<BusinessTaskWaveOptionRow> result = _tasks
                .Where(task => task.CreatedTimeLocal >= startTimeLocal && task.CreatedTimeLocal < endTimeLocal)
                .GroupBy(task => NormalizeWaveCode(task.WaveCode), StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var waveRemark = group
                        .Where(task => !string.IsNullOrWhiteSpace(task.WaveRemark))
                        .OrderByDescending(task => task.UpdatedTimeLocal)
                        .Select(task => task.WaveRemark!.Trim())
                        .FirstOrDefault();
                    return new BusinessTaskWaveOptionRow
                    {
                        WaveCode = group.Key,
                        WaveRemark = waveRemark
                    };
                })
                .OrderBy(row => row.WaveCode, StringComparer.Ordinal)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<IReadOnlyList<BusinessTaskEntity>> FindByWaveCodeAndCreatedTimeRangeAsync(DateTime startTimeLocal, DateTime endTimeLocal, string waveCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(waveCode))
        {
            return Task.FromResult<IReadOnlyList<BusinessTaskEntity>>([]);
        }

        lock (_gate)
        {
            IReadOnlyList<BusinessTaskEntity> result = _tasks
                .Where(task => task.CreatedTimeLocal >= startTimeLocal && task.CreatedTimeLocal < endTimeLocal)
                .Where(task => string.Equals(NormalizeWaveCode(task.WaveCode), waveCode.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(task => task.CreatedTimeLocal)
                .ToList();
            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// 执行 ListWaveTaskStatsByWaveCodeAndCreatedTimeRangeAsync 方法。
    /// </summary>
    public Task<IReadOnlyList<BusinessTaskWaveTaskStatsRow>> ListWaveTaskStatsByWaveCodeAndCreatedTimeRangeAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string waveCode,
        CancellationToken ct)
    {
        // 步骤：执行 ListWaveTaskStatsByWaveCodeAndCreatedTimeRangeAsync 方法的核心处理流程。
        if (string.IsNullOrWhiteSpace(waveCode))
        {
            return Task.FromResult<IReadOnlyList<BusinessTaskWaveTaskStatsRow>>([]);
        }

        lock (_gate)
        {
            IReadOnlyList<BusinessTaskWaveTaskStatsRow> result = _tasks
                .Where(task => task.CreatedTimeLocal >= startTimeLocal && task.CreatedTimeLocal < endTimeLocal)
                .Where(task => string.Equals(NormalizeWaveCode(task.WaveCode), waveCode.Trim(), StringComparison.OrdinalIgnoreCase))
                .Select(task => new BusinessTaskWaveTaskStatsRow
                {
                    TaskCode = task.TaskCode,
                    WaveCode = NormalizeWaveCode(task.WaveCode),
                    SourceType = task.SourceType,
                    WorkingArea = task.WorkingArea,
                    Status = task.Status,
                    ResolvedDockCode = task.ResolvedDockCode,
                    IsException = task.IsException,
                    WaveRemark = task.WaveRemark,
                    UpdatedTimeLocal = task.UpdatedTimeLocal
                })
                .ToList();
            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// 执行 AggregateDockDashboardAsync 方法。
    /// </summary>
    public Task<IReadOnlyList<BusinessTaskDockAggregateRow>> AggregateDockDashboardAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? waveCode,
        string? dockCode,
        CancellationToken ct)
    {
        // 步骤：执行 AggregateDockDashboardAsync 方法的核心处理流程。
        lock (_gate)
        {
            var query = _tasks
                .Where(task => task.CreatedTimeLocal >= startTimeLocal && task.CreatedTimeLocal < endTimeLocal);
            if (!string.IsNullOrWhiteSpace(waveCode))
            {
                query = query.Where(task => string.Equals(NormalizeWaveCode(task.WaveCode), waveCode, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(dockCode))
            {
                query = query.Where(task => string.Equals(ResolveDockCode(task), dockCode, StringComparison.OrdinalIgnoreCase));
            }

            IReadOnlyList<BusinessTaskDockAggregateRow> result = query
                .GroupBy(task => ResolveDockCode(task), StringComparer.OrdinalIgnoreCase)
                .Select(group => new BusinessTaskDockAggregateRow
                {
                    DockCode = group.Key,
                    TotalCount = group.Count(),
                    SortedCount = group.Count(task => IsSorted(task)),
                    SplitUnsortedCount = group.Count(task => task.SourceType == BusinessTaskSourceType.Split && !IsSorted(task)),
                    FullCaseUnsortedCount = group.Count(task => task.SourceType == BusinessTaskSourceType.FullCase && !IsSorted(task)),
                    SplitTotalCount = group.Count(task => task.SourceType == BusinessTaskSourceType.Split),
                    FullCaseTotalCount = group.Count(task => task.SourceType == BusinessTaskSourceType.FullCase),
                    SplitSortedCount = group.Count(task => task.SourceType == BusinessTaskSourceType.Split && IsSorted(task)),
                    FullCaseSortedCount = group.Count(task => task.SourceType == BusinessTaskSourceType.FullCase && IsSorted(task)),
                    RecirculatedCount = group.Count(task => _queryPolicy.IsRecirculatedByResolvedDockCode(task.ResolvedDockCode)),
                    ExceptionCount = group.Count(task => task.IsException || task.Status == BusinessTaskStatus.Exception)
                })
                .OrderBy(row => row.DockCode, StringComparer.Ordinal)
                .ToList();
            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// 执行 AggregateRecirculationSummaryAsync 方法。
    /// </summary>
    public Task<IReadOnlyList<BusinessTaskRecirculationAggregateRow>> AggregateRecirculationSummaryAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? chuteCode,
        CancellationToken ct)
    {
        // 步骤：执行 AggregateRecirculationSummaryAsync 方法的核心处理流程。
        lock (_gate)
        {
            var query = _tasks
                .Where(task => task.CreatedTimeLocal >= startTimeLocal && task.CreatedTimeLocal < endTimeLocal)
                .Where(task => _queryPolicy.IsRecirculatedByResolvedDockCode(task.ResolvedDockCode));
            if (!string.IsNullOrWhiteSpace(chuteCode))
            {
                query = query.Where(task => string.Equals(ResolveDockCode(task), chuteCode.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            IReadOnlyList<BusinessTaskRecirculationAggregateRow> result = query
                .GroupBy(task => new
                {
                    ChuteCode = ResolveDockCode(task),
                    WaveCode = NormalizeWaveCode(task.WaveCode)
                })
                .Select(group => new BusinessTaskRecirculationAggregateRow
                {
                    ChuteCode = group.Key.ChuteCode,
                    WaveCode = group.Key.WaveCode,
                    RecirculatedCount = group.Count()
                })
                .OrderBy(row => row.ChuteCode, StringComparer.Ordinal)
                .ThenBy(row => row.WaveCode, StringComparer.Ordinal)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<int> CountByQueryConditionsAsync(BusinessTaskSearchFilter filter, CancellationToken ct)
    {
        lock (_gate)
        {
            return Task.FromResult(BuildFilterQuery(filter).Count());
        }
    }

    public Task<IReadOnlyList<BusinessTaskEntity>> QueryByQueryConditionsAsync(BusinessTaskSearchFilter filter, int skip, int take, CancellationToken ct)
    {
        lock (_gate)
        {
            IReadOnlyList<BusinessTaskEntity> result = BuildFilterQuery(filter)
                .OrderByDescending(task => task.CreatedTimeLocal)
                .ThenByDescending(task => task.Id)
                .Skip(skip)
                .Take(take)
                .ToList();
            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// 执行 QueryByCursorConditionsAsync 方法。
    /// </summary>
    public Task<IReadOnlyList<BusinessTaskEntity>> QueryByCursorConditionsAsync(
        BusinessTaskSearchFilter filter,
        DateTime? lastCreatedTimeLocal,
        long? lastId,
        int take,
        CancellationToken ct)
    {
        // 步骤：执行 QueryByCursorConditionsAsync 方法的核心处理流程。
        lock (_gate)
        {
            var query = BuildFilterQuery(filter)
                .OrderByDescending(task => task.CreatedTimeLocal)
                .ThenByDescending(task => task.Id);
            if (lastCreatedTimeLocal.HasValue && lastId.HasValue)
            {
                var cursorCreatedTime = lastCreatedTimeLocal.Value;
                var cursorId = lastId.Value;
                query = query.Where(task =>
                        task.CreatedTimeLocal < cursorCreatedTime
                        || (task.CreatedTimeLocal == cursorCreatedTime && task.Id < cursorId))
                    .OrderByDescending(task => task.CreatedTimeLocal)
                    .ThenByDescending(task => task.Id);
            }

            IReadOnlyList<BusinessTaskEntity> result = query.Take(take).ToList();
            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// 执行 QueryPageWithTotalCountByConditionsAsync 方法。
    /// </summary>
    public Task<(int TotalCount, IReadOnlyList<BusinessTaskEntity> Items)> QueryPageWithTotalCountByConditionsAsync(
        BusinessTaskSearchFilter filter,
        int skip,
        int take,
        CancellationToken ct)
    {
        // 步骤：执行 QueryPageWithTotalCountByConditionsAsync 方法的核心处理流程。
        lock (_gate)
        {
            var query = BuildFilterQuery(filter)
                .OrderByDescending(task => task.CreatedTimeLocal)
                .ThenByDescending(task => task.Id);
            var totalCount = query.Count();
            IReadOnlyList<BusinessTaskEntity> items = query.Skip(skip).Take(take).ToList();
            return Task.FromResult((totalCount, items));
        }
    }

    private IEnumerable<BusinessTaskEntity> BuildFilterQuery(BusinessTaskSearchFilter filter)
    {
        var query = _tasks
            .Where(task => task.CreatedTimeLocal >= filter.StartTimeLocal && task.CreatedTimeLocal < filter.EndTimeLocal);
        if (!string.IsNullOrWhiteSpace(filter.WaveCode))
        {
            if (string.Equals(filter.WaveCode, EmptyWaveCode, StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(task => task.NormalizedWaveCode == null);
            }
            else
            {
                query = query.Where(task => string.Equals(task.NormalizedWaveCode, filter.WaveCode, StringComparison.OrdinalIgnoreCase));
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.Barcode))
        {
            query = query.Where(task => string.Equals(task.NormalizedBarcode, filter.Barcode, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.DockCode))
        {
            query = query.Where(task => string.Equals(task.ResolvedDockCode, filter.DockCode, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.ChuteCode))
        {
            query = query.Where(task =>
                string.Equals(task.TargetChuteCode, filter.ChuteCode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(task.ActualChuteCode, filter.ChuteCode, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.OnlyException)
        {
            query = query.Where(task => task.IsException || task.Status == BusinessTaskStatus.Exception);
        }

        if (filter.OnlyRecirculation)
        {
            query = query.Where(task => _queryPolicy.IsRecirculatedByResolvedDockCode(task.ResolvedDockCode));
        }

        return query;
    }

    private static bool IsAllowedScanTransitionSourceStatus(BusinessTaskStatus status)
    {
        return status is BusinessTaskStatus.Created
            or BusinessTaskStatus.Scanned
            or BusinessTaskStatus.Dropped;
    }

    private static bool IsSorted(BusinessTaskEntity task)
    {
        return task.Status == BusinessTaskStatus.Dropped || task.Status == BusinessTaskStatus.FeedbackPending;
    }

    private static string NormalizeWaveCode(string? waveCode)
    {
        return string.IsNullOrWhiteSpace(waveCode) ? EmptyWaveCode : waveCode.Trim();
    }

    private static string ResolveDockCode(BusinessTaskEntity task)
    {
        if (!string.IsNullOrWhiteSpace(task.ActualChuteCode))
        {
            return task.ActualChuteCode.Trim();
        }

        if (!string.IsNullOrWhiteSpace(task.TargetChuteCode))
        {
            return task.TargetChuteCode.Trim();
        }

        return EmptyDockCode;
    }
}

