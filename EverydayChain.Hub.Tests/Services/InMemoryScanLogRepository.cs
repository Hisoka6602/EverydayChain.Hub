using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 扫描日志仓储内存替身，用于单元测试。
/// </summary>
internal sealed class InMemoryScanLogRepository : IScanLogRepository
{
    /// <summary>内存日志存储。</summary>
    public List<ScanLogEntity> Logs { get; } = [];

    /// <summary>自增 Id 计数器。</summary>
    private long _nextId = 1;

    /// <inheritdoc/>
    public Task SaveAsync(ScanLogEntity entity, CancellationToken ct)
    {
        entity.Id = _nextId++;
        Logs.Add(entity);
        return Task.CompletedTask;
    }
}
