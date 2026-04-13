using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 落格回传应用服务骨架实现。
/// </summary>
public sealed class DropFeedbackService : IDropFeedbackService {
    /// <summary>
    /// 处理落格回传并返回标准结果。
    /// </summary>
    /// <param name="request">落格回传请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>处理结果。</returns>
    public Task<DropFeedbackApplicationResult> ExecuteAsync(DropFeedbackApplicationRequest request, CancellationToken cancellationToken) {
        _ = cancellationToken;
        var normalizedTaskCode = string.IsNullOrWhiteSpace(request.TaskCode)
            ? $"TASK-{request.Barcode.Trim()}"
            : request.TaskCode.Trim();
        var result = new DropFeedbackApplicationResult {
            IsAccepted = true,
            TaskCode = normalizedTaskCode,
            Status = "FeedbackPending",
            Message = "落格回传已受理，后续阶段将接入真实状态机与业务回传。"
        };

        return Task.FromResult(result);
    }
}
