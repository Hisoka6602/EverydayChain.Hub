using EverydayChain.Hub.Domain.Recirculation;

namespace EverydayChain.Hub.Application.Recirculation.Abstractions;

/// <summary>
/// 定义 IRecirculationService 类型。
/// </summary>
public interface IRecirculationService
{
    /// <summary>
    /// 执行 EvaluateAsync 方法。
    /// </summary>
    Task<RecirculationDecisionResult> EvaluateAsync(long taskId, CancellationToken ct);
}

