using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;

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

    /// <summary>
    /// 查询回传状态为"待回传"的业务任务列表，按创建时间升序。
    /// </summary>
    /// <param name="maxCount">最多返回的记录数。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>待回传的业务任务列表。</returns>
    Task<IReadOnlyList<BusinessTaskEntity>> FindPendingFeedbackAsync(int maxCount, CancellationToken ct);

    /// <summary>
    /// 查询回传状态为"回传失败"的业务任务列表，按创建时间升序（用于补偿重试）。
    /// </summary>
    /// <param name="maxCount">最多返回的记录数。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>回传失败的业务任务列表。</returns>
    Task<IReadOnlyList<BusinessTaskEntity>> FindFailedFeedbackAsync(int maxCount, CancellationToken ct);

    /// <summary>
    /// 按波次编码批量更新所有非终态业务任务的状态、失败原因与更新时间。
    /// 在单次数据库往返内完成，用于波次清理场景的高效批处理。
    /// </summary>
    /// <param name="waveCode">波次编码。</param>
    /// <param name="targetStatus">目标状态。</param>
    /// <param name="failureReasonPrefix">失败原因前缀，实现时可在末尾附加原状态或波次信息。</param>
    /// <param name="updatedTimeLocal">更新时间（本地时间）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>实际更新的行数。</returns>
    Task<int> BulkMarkExceptionByWaveCodeAsync(
        string waveCode,
        BusinessTaskStatus targetStatus,
        string failureReasonPrefix,
        DateTime updatedTimeLocal,
        CancellationToken ct);

    /// <summary>
    /// 按波次编码查询所有业务任务（包括终态与非终态），按创建时间升序。
    /// </summary>
    /// <param name="waveCode">波次编码。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>该波次的所有业务任务列表。</returns>
    Task<IReadOnlyList<BusinessTaskEntity>> FindByWaveCodeAsync(string waveCode, CancellationToken ct);

    /// <summary>
    /// 按条码查询所有非终态业务任务，用于多标签场景检测，按创建时间升序。
    /// </summary>
    /// <param name="barcode">条码文本。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>该条码关联的所有非终态业务任务列表。</returns>
    Task<IReadOnlyList<BusinessTaskEntity>> FindActiveByBarcodeAsync(string barcode, CancellationToken ct);

    /// <summary>
    /// 按创建时间区间查询业务任务列表（左闭右开），按创建时间升序返回。
    /// </summary>
    /// <param name="startTimeLocal">区间起始本地时间（包含）。</param>
    /// <param name="endTimeLocal">区间结束本地时间（不包含）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>命中的业务任务列表。</returns>
    Task<IReadOnlyList<BusinessTaskEntity>> FindByCreatedTimeRangeAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct);
}
