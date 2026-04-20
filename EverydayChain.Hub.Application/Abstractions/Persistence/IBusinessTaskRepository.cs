using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Application.Models;

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
    /// 按来源表编码与业务键查找业务任务；未找到返回 <c>null</c>。
    /// </summary>
    /// <param name="sourceTableCode">来源表编码。</param>
    /// <param name="businessKey">业务键。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>业务任务实体或 <c>null</c>。</returns>
    Task<BusinessTaskEntity?> FindBySourceTableAndBusinessKeyAsync(string sourceTableCode, string businessKey, CancellationToken ct);

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
    /// 按投影规则执行幂等 Upsert。
    /// </summary>
    /// <param name="entity">投影后的业务任务实体。</param>
    /// <param name="ct">取消令牌。</param>
    Task UpsertProjectionAsync(BusinessTaskEntity entity, CancellationToken ct);

    /// <summary>
    /// 按投影规则执行批量幂等 Upsert。
    /// </summary>
    /// <param name="entities">投影后的业务任务实体集合。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>实际处理的实体数量。</returns>
    Task<int> UpsertProjectionBatchAsync(IReadOnlyList<BusinessTaskEntity> entities, CancellationToken ct);

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

    /// <summary>
    /// 按创建时间区间聚合总看板所需的波次统计数据（左闭右开）。
    /// 聚合在仓储侧优先完成，应用层仅做最终口径拼装。
    /// </summary>
    /// <param name="startTimeLocal">区间起始本地时间（包含）。</param>
    /// <param name="endTimeLocal">区间结束本地时间（不包含）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>波次聚合行集合。</returns>
    Task<IReadOnlyList<BusinessTaskWaveAggregateRow>> AggregateWaveDashboardAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct);

    /// <summary>
    /// 列出创建时间区间内的波次编码选项（左闭右开），用于码头看板波次筛选。
    /// </summary>
    /// <param name="startTimeLocal">区间起始本地时间（包含）。</param>
    /// <param name="endTimeLocal">区间结束本地时间（不包含）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>归一化波次编码集合。</returns>
    Task<IReadOnlyList<string>> ListWaveCodesByCreatedTimeRangeAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct);

    /// <summary>
    /// 列出创建时间区间内的波次选项（波次号与备注，左闭右开），用于波次下拉查询。
    /// </summary>
    /// <param name="startTimeLocal">区间起始本地时间（包含）。</param>
    /// <param name="endTimeLocal">区间结束本地时间（不包含）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>波次选项集合。</returns>
    Task<IReadOnlyList<BusinessTaskWaveOptionRow>> ListWaveOptionsByCreatedTimeRangeAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct);

    /// <summary>
    /// 按创建时间区间与波次编码查询业务任务列表（左闭右开），按创建时间升序返回。
    /// </summary>
    /// <param name="startTimeLocal">区间起始本地时间（包含）。</param>
    /// <param name="endTimeLocal">区间结束本地时间（不包含）。</param>
    /// <param name="waveCode">波次编码。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>命中的业务任务列表。</returns>
    Task<IReadOnlyList<BusinessTaskEntity>> FindByWaveCodeAndCreatedTimeRangeAsync(DateTime startTimeLocal, DateTime endTimeLocal, string waveCode, CancellationToken ct);

    /// <summary>
    /// 按创建时间区间聚合码头看板/报表所需统计数据（左闭右开），并可按波次或码头过滤。
    /// 聚合在仓储侧优先完成，应用层仅处理 7 号码头异常规则等口径细节。
    /// </summary>
    /// <param name="startTimeLocal">区间起始本地时间（包含）。</param>
    /// <param name="endTimeLocal">区间结束本地时间（不包含）。</param>
    /// <param name="waveCode">可选波次编码。</param>
    /// <param name="dockCode">可选码头编码。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>码头聚合行集合。</returns>
    Task<IReadOnlyList<BusinessTaskDockAggregateRow>> AggregateDockDashboardAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? waveCode,
        string? dockCode,
        CancellationToken ct);

    /// <summary>
    /// 按查询条件统计业务任务总数。
    /// 过滤条件在仓储侧下推执行，不在应用层做全量内存过滤。
    /// </summary>
    /// <param name="filter">查询过滤条件。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>命中的业务任务总数。</returns>
    Task<int> CountByQueryConditionsAsync(BusinessTaskSearchFilter filter, CancellationToken ct);

    /// <summary>
    /// 按查询条件读取分页业务任务列表，排序与分页在仓储侧下推执行。
    /// </summary>
    /// <param name="filter">查询过滤条件。</param>
    /// <param name="skip">跳过条数。</param>
    /// <param name="take">读取条数。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>命中的分页业务任务列表。</returns>
    Task<IReadOnlyList<BusinessTaskEntity>> QueryByQueryConditionsAsync(
        BusinessTaskSearchFilter filter,
        int skip,
        int take,
        CancellationToken ct);

    /// <summary>
    /// 按查询条件读取游标分页业务任务列表，排序固定为创建时间降序、主键降序。
    /// </summary>
    /// <param name="filter">查询过滤条件。</param>
    /// <param name="lastCreatedTimeLocal">上一页最后一条创建时间（本地时间）。</param>
    /// <param name="lastId">上一页最后一条主键 Id。</param>
    /// <param name="take">读取条数。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>命中的分页业务任务列表。</returns>
    Task<IReadOnlyList<BusinessTaskEntity>> QueryByCursorConditionsAsync(
        BusinessTaskSearchFilter filter,
        DateTime? lastCreatedTimeLocal,
        long? lastId,
        int take,
        CancellationToken ct);

    /// <summary>
    /// 按查询条件读取页码分页结果，并在单次跨分表遍历中返回总数。
    /// </summary>
    /// <param name="filter">查询过滤条件。</param>
    /// <param name="skip">跳过条数。</param>
    /// <param name="take">读取条数。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>总数与结果列表。</returns>
    Task<(int TotalCount, IReadOnlyList<BusinessTaskEntity> Items)> QueryPageWithTotalCountByConditionsAsync(
        BusinessTaskSearchFilter filter,
        int skip,
        int take,
        CancellationToken ct);
}
