using EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 扫描日志仓储抽象，定义对 <see cref="ScanLogEntity"/> 的持久化写入契约。
/// </summary>
public interface IScanLogRepository
{
    /// <summary>
    /// 新增扫描日志并持久化。
    /// </summary>
    /// <param name="entity">扫描日志实体。</param>
    /// <param name="ct">取消令牌。</param>
    Task SaveAsync(ScanLogEntity entity, CancellationToken ct);
}
