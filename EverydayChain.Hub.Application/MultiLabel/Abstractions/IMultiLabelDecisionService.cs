using EverydayChain.Hub.Domain.MultiLabel;

namespace EverydayChain.Hub.Application.MultiLabel.Abstractions;

/// <summary>
/// 多标签决策服务接口，负责识别同一条码关联多个业务任务的场景并输出处置决策。
/// </summary>
public interface IMultiLabelDecisionService
{
    /// <summary>
    /// 对给定条码的所有关联业务任务执行多标签决策。
    /// 若关联任务数为 1 则直接返回非多标签结论；否则根据配置策略决定选用/舍弃哪个任务。
    /// </summary>
    /// <param name="barcode">条码文本，不能为空白。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>多标签决策结果。</returns>
    Task<MultiLabelDecisionResult> DecideAsync(string barcode, CancellationToken ct);
}
