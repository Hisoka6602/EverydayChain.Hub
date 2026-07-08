using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义 IWmsFeedbackService 类型。
/// </summary>
public interface IWmsFeedbackService
{
    /// <summary>
    /// 执行 ExecuteAsync 方法。
    /// </summary>
    Task<WmsFeedbackApplicationResult> ExecuteAsync(int batchSize, CancellationToken ct);
}

