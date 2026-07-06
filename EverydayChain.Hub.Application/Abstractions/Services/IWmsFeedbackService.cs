using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IWmsFeedbackService
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<WmsFeedbackApplicationResult> ExecuteAsync(int batchSize, CancellationToken ct);
}

