using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IBusinessTaskRepository
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<BusinessTaskEntity?> FindByBarcodeAsync(string barcode, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<BusinessTaskEntity?> FindByTaskCodeAsync(string taskCode, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<BusinessTaskEntity?> FindBySourceTableAndBusinessKeyAsync(string sourceTableCode, string businessKey, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<BusinessTaskEntity?> FindByIdAsync(long id, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<IReadOnlyDictionary<long, BusinessTaskEntity>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task SaveAsync(BusinessTaskEntity entity, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task UpsertProjectionAsync(BusinessTaskEntity entity, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<int> UpsertProjectionBatchAsync(IReadOnlyList<BusinessTaskEntity> entities, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task UpdateAsync(BusinessTaskEntity entity, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<bool> TryMarkScannedAsync(long taskId, DateTime createdTimeLocal, BusinessTaskScanUpdateCommand command, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<bool> IncrementScanRetryAsync(long taskId, DateTime createdTimeLocal, DateTime updatedTimeLocal, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<IReadOnlyList<BusinessTaskEntity>> FindPendingFeedbackAsync(int maxCount, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<IReadOnlyList<BusinessTaskEntity>> FindFailedFeedbackAsync(int maxCount, CancellationToken ct);

    Task<IReadOnlyList<BusinessTaskEntity>> ClaimFeedbackBatchAsync(
        BusinessTaskFeedbackStatus sourceStatus,
        int maxCount,
        DateTime claimedTimeLocal,
        TimeSpan staleAfter,
        CancellationToken ct);

    Task<BusinessTaskEntity?> ClaimFeedbackByTaskCodeAsync(
        string taskCode,
        DateTime claimedTimeLocal,
        TimeSpan staleAfter,
        CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<int> CompleteClaimedFeedbackBatchAsync(IReadOnlyCollection<long> ids, DateTime completedTimeLocal, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<int> FailClaimedFeedbackBatchAsync(IReadOnlyCollection<long> ids, DateTime failedTimeLocal, CancellationToken ct);

    Task<int> BulkMarkExceptionByWaveCodeAsync(
        string waveCode,
        BusinessTaskStatus targetStatus,
        string failureReasonPrefix,
        DateTime updatedTimeLocal,
        CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<IReadOnlyList<BusinessTaskEntity>> FindByWaveCodeAsync(string waveCode, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<IReadOnlyList<BusinessTaskEntity>> FindActiveByBarcodeAsync(string barcode, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<IReadOnlyList<BusinessTaskEntity>> FindByCreatedTimeRangeAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct);

    Task<IReadOnlyList<BusinessTaskProjectionBackfillCandidate>> FindProjectionBackfillCandidatesAsync(
        string sourceTableCode,
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        bool requireOrderId,
        bool requireStoreId,
        bool requireStoreName,
        bool requireProductCode,
        bool requirePickLocation,
        int take,
        CancellationToken ct);

    Task<BusinessTaskProjectionGapSummary> CountProjectionBackfillGapsAsync(
        string sourceTableCode,
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        bool requireOrderId,
        bool requireStoreId,
        bool requireStoreName,
        bool requireProductCode,
        bool requirePickLocation,
        CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<BusinessTaskEntity?> FindLatestScannedWithWaveByCreatedTimeRangeAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<IReadOnlyList<BusinessTaskWaveAggregateRow>> AggregateWaveDashboardAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<BusinessTaskFeedbackAggregate> AggregateFeedbackAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<IReadOnlyList<string>> ListWaveCodesByCreatedTimeRangeAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<IReadOnlyList<BusinessTaskWaveOptionRow>> ListWaveOptionsByCreatedTimeRangeAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<IReadOnlyList<BusinessTaskEntity>> FindByWaveCodeAndCreatedTimeRangeAsync(DateTime startTimeLocal, DateTime endTimeLocal, string waveCode, CancellationToken ct);

    Task<IReadOnlyList<BusinessTaskWaveTaskStatsRow>> ListWaveTaskStatsByWaveCodeAndCreatedTimeRangeAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string waveCode,
        CancellationToken ct);

    Task<IReadOnlyList<BusinessTaskDockAggregateRow>> AggregateDockDashboardAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? waveCode,
        string? dockCode,
        CancellationToken ct);

    Task<IReadOnlyList<BusinessTaskRecirculationAggregateRow>> AggregateRecirculationSummaryAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? chuteCode,
        CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<int> CountByQueryConditionsAsync(BusinessTaskSearchFilter filter, CancellationToken ct);

    Task<IReadOnlyList<BusinessTaskEntity>> QueryByQueryConditionsAsync(
        BusinessTaskSearchFilter filter,
        int skip,
        int take,
        CancellationToken ct);

    Task<IReadOnlyList<BusinessTaskEntity>> QueryByCursorConditionsAsync(
        BusinessTaskSearchFilter filter,
        DateTime? lastCreatedTimeLocal,
        long? lastId,
        int take,
        CancellationToken ct);

    Task<(int TotalCount, IReadOnlyList<BusinessTaskEntity> Items)> QueryPageWithTotalCountByConditionsAsync(
        BusinessTaskSearchFilter filter,
        int skip,
        int take,
        CancellationToken ct);
}

