using EverydayChain.Hub.Domain.MultiLabel;

namespace EverydayChain.Hub.Application.MultiLabel.Abstractions;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IMultiLabelDecisionService
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<MultiLabelDecisionResult> DecideAsync(string barcode, CancellationToken ct);
}

