using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 业务任务仓储内存替身，用于单元测试。
/// </summary>
internal sealed class InMemoryBusinessTaskRepository : IBusinessTaskRepository
{
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
        entity.Id = _nextId++;
        _tasks.Add(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UpdateAsync(BusinessTaskEntity entity, CancellationToken ct)
    {
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
}
