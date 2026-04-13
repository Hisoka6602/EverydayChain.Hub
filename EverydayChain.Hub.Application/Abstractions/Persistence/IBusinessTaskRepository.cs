using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 业务任务仓储抽象，定义对 <see cref="BusinessTaskEntity"/> 的持久化协作契约。
/// </summary>
public interface IBusinessTaskRepository
{
    /// <summary>
    /// 按条码查找业务任务；未找到返回 <c>null</c>。
    /// </summary>
    /// <param name="barcode">条码文本。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>业务任务实体或 <c>null</c>。</returns>
    Task<BusinessTaskEntity?> FindByBarcodeAsync(string barcode, CancellationToken ct);

    /// <summary>
    /// 按任务编码查找业务任务；未找到返回 <c>null</c>。
    /// </summary>
    /// <param name="taskCode">任务编码。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>业务任务实体或 <c>null</c>。</returns>
    Task<BusinessTaskEntity?> FindByTaskCodeAsync(string taskCode, CancellationToken ct);

    /// <summary>
    /// 按主键 Id 查找业务任务；未找到返回 <c>null</c>。
    /// </summary>
    /// <param name="id">主键标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>业务任务实体或 <c>null</c>。</returns>
    Task<BusinessTaskEntity?> FindByIdAsync(long id, CancellationToken ct);

    /// <summary>
    /// 新增业务任务并持久化。
    /// </summary>
    /// <param name="entity">业务任务实体。</param>
    /// <param name="ct">取消令牌。</param>
    Task SaveAsync(BusinessTaskEntity entity, CancellationToken ct);

    /// <summary>
    /// 更新已有业务任务并持久化。
    /// </summary>
    /// <param name="entity">业务任务实体（Id 必须有效）。</param>
    /// <param name="ct">取消令牌。</param>
    Task UpdateAsync(BusinessTaskEntity entity, CancellationToken ct);
}
