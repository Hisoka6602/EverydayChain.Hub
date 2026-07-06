using EverydayChain.Hub.Domain.Aggregates.DropLogAggregate;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IDropLogRepository
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task SaveAsync(DropLogEntity entity, CancellationToken ct);
}

