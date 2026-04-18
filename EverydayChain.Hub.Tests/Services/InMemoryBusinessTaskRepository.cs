using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 业务任务仓储内存替身，用于单元测试。
/// </summary>
internal sealed class InMemoryBusinessTaskRepository : IBusinessTaskRepository
{
    /// <summary>无波次占位文本。</summary>
    private const string EmptyWaveCode = "未分波次";

    /// <summary>无码头占位文本。</summary>
    private const string EmptyDockCode = "未分配码头";

    /// <summary>内存任务存储。</summary>
    private readonly List<BusinessTaskEntity> _tasks = [];

    /// <summary>自增 Id 计数器。</summary>
    private long _nextId = 1;

    /// <inheritdoc/>
    public Task<BusinessTaskEntity?> FindByBarcodeAsync(string barcode, CancellationToken ct)
    {
        var task = _tasks.FirstOrDefault(x => string.Equals(x.Barcode, barcode, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(task);
    }

    /// <inheritdoc/>
    public Task<BusinessTaskEntity?> FindByTaskCodeAsync(string taskCode, CancellationToken ct)
    {
        var task = _tasks.FirstOrDefault(x => string.Equals(x.TaskCode, taskCode, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(task);
    }

    /// <inheritdoc/>
    public Task<BusinessTaskEntity?> FindByIdAsync(long id, CancellationToken ct)
    {
        var task = _tasks.FirstOrDefault(x => x.Id == id);
        return Task.FromResult(task);
    }

    /// <inheritdoc/>
    public Task SaveAsync(BusinessTaskEntity entity, CancellationToken ct)
    {
        entity.RefreshQueryFields();
        entity.Id = _nextId++;
        _tasks.Add(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UpdateAsync(BusinessTaskEntity entity, CancellationToken ct)
    {
        entity.RefreshQueryFields();
        var idx = _tasks.FindIndex(x => x.Id == entity.Id);
        if (idx >= 0)
        {
            _tasks[idx] = entity;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<BusinessTaskEntity>> FindPendingFeedbackAsync(int maxCount, CancellationToken ct)
    {
        IReadOnlyList<BusinessTaskEntity> result = _tasks
            .Where(x => x.FeedbackStatus == BusinessTaskFeedbackStatus.Pending)
            .OrderBy(x => x.CreatedTimeLocal)
            .Take(maxCount)
            .ToList();
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<BusinessTaskEntity>> FindFailedFeedbackAsync(int maxCount, CancellationToken ct)
    {
        IReadOnlyList<BusinessTaskEntity> result = _tasks
            .Where(x => x.FeedbackStatus == BusinessTaskFeedbackStatus.Failed)
            .OrderBy(x => x.CreatedTimeLocal)
            .Take(maxCount)
            .ToList();
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<int> BulkMarkExceptionByWaveCodeAsync(
        string waveCode,
        BusinessTaskStatus targetStatus,
        string failureReasonPrefix,
        DateTime updatedTimeLocal,
        CancellationToken ct)
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

    /// <inheritdoc/>
    public Task<IReadOnlyList<BusinessTaskEntity>> FindByWaveCodeAsync(string waveCode, CancellationToken ct)
    {
        IReadOnlyList<BusinessTaskEntity> result = _tasks
            .Where(x => string.Equals(x.WaveCode, waveCode, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.CreatedTimeLocal)
            .ToList();
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<BusinessTaskEntity>> FindActiveByBarcodeAsync(string barcode, CancellationToken ct)
    {
        IReadOnlyList<BusinessTaskEntity> result = _tasks
            .Where(x => string.Equals(x.Barcode, barcode, StringComparison.OrdinalIgnoreCase)
                && x.Status != BusinessTaskStatus.Dropped
                && x.Status != BusinessTaskStatus.Exception)
            .OrderBy(x => x.CreatedTimeLocal)
            .ToList();
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<BusinessTaskEntity>> FindByCreatedTimeRangeAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct)
    {
        IReadOnlyList<BusinessTaskEntity> result = _tasks
            .Where(x => x.CreatedTimeLocal >= startTimeLocal && x.CreatedTimeLocal < endTimeLocal)
            .OrderBy(x => x.CreatedTimeLocal)
            .ToList();
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<BusinessTaskWaveAggregateRow>> AggregateWaveDashboardAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct)
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
                RecirculatedCount = group.Count(task => task.IsRecirculated),
                ExceptionCount = group.Count(task => task.IsException || task.Status == BusinessTaskStatus.Exception),
                TotalVolumeMm3 = group.Sum(task => task.VolumeMm3 ?? 0M),
                TotalWeightGram = group.Sum(task => task.WeightGram ?? 0M)
            })
            .OrderBy(row => row.WaveCode, StringComparer.Ordinal)
            .ToList();
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> ListWaveCodesByCreatedTimeRangeAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct)
    {
        IReadOnlyList<string> result = _tasks
            .Where(task => task.CreatedTimeLocal >= startTimeLocal && task.CreatedTimeLocal < endTimeLocal)
            .Select(task => NormalizeWaveCode(task.WaveCode))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<BusinessTaskDockAggregateRow>> AggregateDockDashboardAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? waveCode,
        string? dockCode,
        CancellationToken ct)
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
                RecirculatedCount = group.Count(task => task.IsRecirculated),
                ExceptionCount = group.Count(task => task.IsException || task.Status == BusinessTaskStatus.Exception)
            })
            .OrderBy(row => row.DockCode, StringComparer.Ordinal)
            .ToList();
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<int> CountByQueryConditionsAsync(BusinessTaskSearchFilter filter, CancellationToken ct)
    {
        var count = BuildFilterQuery(filter).Count();
        return Task.FromResult(count);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<BusinessTaskEntity>> QueryByQueryConditionsAsync(BusinessTaskSearchFilter filter, int skip, int take, CancellationToken ct)
    {
        IReadOnlyList<BusinessTaskEntity> result = BuildFilterQuery(filter)
            .OrderByDescending(task => task.CreatedTimeLocal)
            .ThenByDescending(task => task.Id)
            .Skip(skip)
            .Take(take)
            .ToList();
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<BusinessTaskEntity>> QueryByCursorConditionsAsync(
        BusinessTaskSearchFilter filter,
        DateTime? lastCreatedTimeLocal,
        long? lastId,
        int take,
        CancellationToken ct)
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

    /// <inheritdoc/>
    public Task<(int TotalCount, IReadOnlyList<BusinessTaskEntity> Items)> QueryPageWithTotalCountByConditionsAsync(
        BusinessTaskSearchFilter filter,
        int skip,
        int take,
        CancellationToken ct)
    {
        var query = BuildFilterQuery(filter)
            .OrderByDescending(task => task.CreatedTimeLocal)
            .ThenByDescending(task => task.Id);
        var totalCount = query.Count();
        IReadOnlyList<BusinessTaskEntity> items = query.Skip(skip).Take(take).ToList();
        return Task.FromResult((totalCount, items));
    }

    /// <summary>
    /// 构建过滤查询。
    /// </summary>
    /// <param name="filter">过滤条件。</param>
    /// <returns>过滤后查询。</returns>
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
            query = query.Where(task => string.Equals(task.ResolvedDockCode, filter.ChuteCode, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.OnlyException)
        {
            query = query.Where(task => task.IsException || task.Status == BusinessTaskStatus.Exception);
        }

        if (filter.OnlyRecirculation)
        {
            query = query.Where(task => task.IsRecirculated);
        }

        return query;
    }

    /// <summary>
    /// 判断任务是否已分拣。
    /// </summary>
    /// <param name="task">业务任务。</param>
    /// <returns>已分拣返回 true，否则返回 false。</returns>
    private static bool IsSorted(BusinessTaskEntity task)
    {
        return task.Status == BusinessTaskStatus.Dropped || task.Status == BusinessTaskStatus.FeedbackPending;
    }

    /// <summary>
    /// 归一化波次编码。
    /// </summary>
    /// <param name="waveCode">波次编码。</param>
    /// <returns>归一化后的波次编码。</returns>
    private static string NormalizeWaveCode(string? waveCode)
    {
        return string.IsNullOrWhiteSpace(waveCode) ? EmptyWaveCode : waveCode.Trim();
    }

    /// <summary>
    /// 解析码头编码。
    /// </summary>
    /// <param name="task">业务任务。</param>
    /// <returns>码头编码。</returns>
    private static string ResolveDockCode(BusinessTaskEntity task)
    {
        return task.ResolvedDockCode;
    }
}
