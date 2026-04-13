using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 业务回传应用服务抽象，定义查询待回传任务并执行 WMS 回传的编排契约。
/// </summary>
public interface IWmsFeedbackService
{
    /// <summary>
    /// 批量执行业务回传：查询待回传任务、调用 Oracle 写入器、更新本地回传状态。
    /// </summary>
    /// <param name="batchSize">单次处理批次大小（建议范围：1~1000）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>本次回传执行结果。</returns>
    Task<WmsFeedbackApplicationResult> ExecuteAsync(int batchSize, CancellationToken ct);
}
