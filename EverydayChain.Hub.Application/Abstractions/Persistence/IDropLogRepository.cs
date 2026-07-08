using EverydayChain.Hub.Domain.Aggregates.DropLogAggregate;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义 IDropLogRepository 类型。
/// </summary>
public interface IDropLogRepository
{
    /// <summary>
    /// 执行 SaveAsync 方法。
    /// </summary>
    Task SaveAsync(DropLogEntity entity, CancellationToken ct);
}

