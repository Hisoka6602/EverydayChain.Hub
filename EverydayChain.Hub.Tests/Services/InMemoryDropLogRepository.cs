using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Aggregates.DropLogAggregate;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 InMemoryDropLogRepository 类型。
/// </summary>
internal sealed class InMemoryDropLogRepository : IDropLogRepository
{
    /// <summary>
    /// 获取或设置 Logs。
    /// </summary>
    public List<DropLogEntity> Logs { get; } = [];

    /// <summary>
    /// 存储 _nextId 字段。
    /// </summary>
    private long _nextId = 1;

    public Task SaveAsync(DropLogEntity entity, CancellationToken ct)
    {
        entity.Id = _nextId++;
        Logs.Add(entity);
        return Task.CompletedTask;
    }
}

