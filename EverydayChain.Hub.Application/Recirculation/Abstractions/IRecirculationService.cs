using EverydayChain.Hub.Domain.Recirculation;

namespace EverydayChain.Hub.Application.Recirculation.Abstractions;

/// <summary>
/// 回流规则服务接口，负责判断业务任务是否需要回流并更新任务回流状态。
/// </summary>
public interface IRecirculationService
{
    /// <summary>
    /// 对指定业务任务执行回流判定。
    /// 若判定需要回流且未处于 dry-run 模式，则更新任务的回流状态标志。
    /// </summary>
    /// <param name="taskId">业务任务主键 Id。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>回流决策结果。</returns>
    Task<RecirculationDecisionResult> EvaluateAsync(long taskId, CancellationToken ct);
}
