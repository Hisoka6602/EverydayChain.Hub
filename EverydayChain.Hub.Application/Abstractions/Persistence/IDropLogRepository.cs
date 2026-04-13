using EverydayChain.Hub.Domain.Aggregates.DropLogAggregate;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 落格日志仓储抽象，定义对 <see cref="DropLogEntity"/> 的持久化写入契约。
/// </summary>
public interface IDropLogRepository
{
    /// <summary>
    /// 新增落格日志并持久化。
    /// </summary>
    /// <param name="entity">落格日志实体。</param>
    /// <param name="ct">取消令牌。</param>
    Task SaveAsync(DropLogEntity entity, CancellationToken ct);
}
