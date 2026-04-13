using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;

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
}
