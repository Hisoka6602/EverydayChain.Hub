using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Aggregates.DropLogAggregate;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
internal sealed class InMemoryDropLogRepository : IDropLogRepository
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public List<DropLogEntity> Logs { get; } = [];

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private long _nextId = 1;

    public Task SaveAsync(DropLogEntity entity, CancellationToken ct)
    {
        entity.Id = _nextId++;
        Logs.Add(entity);
        return Task.CompletedTask;
    }
}

