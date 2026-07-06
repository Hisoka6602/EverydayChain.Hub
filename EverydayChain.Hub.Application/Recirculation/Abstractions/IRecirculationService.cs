using EverydayChain.Hub.Domain.Recirculation;

namespace EverydayChain.Hub.Application.Recirculation.Abstractions;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IRecirculationService
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<RecirculationDecisionResult> EvaluateAsync(long taskId, CancellationToken ct);
}

