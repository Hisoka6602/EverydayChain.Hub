using EverydayChain.Hub.Domain.MultiLabel;

namespace EverydayChain.Hub.Application.MultiLabel.Abstractions;

/// <summary>
/// 定义 IMultiLabelDecisionService 类型。
/// </summary>
public interface IMultiLabelDecisionService
{
    /// <summary>
    /// 执行 DecideAsync 方法。
    /// </summary>
    Task<MultiLabelDecisionResult> DecideAsync(string barcode, CancellationToken ct);
}

