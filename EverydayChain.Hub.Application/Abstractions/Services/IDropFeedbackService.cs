using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义 IDropFeedbackService 类型。
/// </summary>
public interface IDropFeedbackService {
    /// <summary>
    /// 执行 ExecuteAsync 方法。
    /// </summary>
    Task<DropFeedbackApplicationResult> ExecuteAsync(DropFeedbackApplicationRequest request, CancellationToken cancellationToken);
}

