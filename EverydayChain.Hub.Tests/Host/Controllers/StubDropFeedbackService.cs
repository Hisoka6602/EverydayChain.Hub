using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 落格回传服务测试替身。
/// </summary>
public sealed class StubDropFeedbackService : IDropFeedbackService {
    /// <summary>
    /// 返回固定测试结果。
    /// </summary>
    /// <param name="request">回传请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>固定结果。</returns>
    public Task<DropFeedbackApplicationResult> ExecuteAsync(DropFeedbackApplicationRequest request, CancellationToken cancellationToken) {
        _ = request;
        _ = cancellationToken;
        return Task.FromResult(new DropFeedbackApplicationResult {
            IsAccepted = true,
            TaskCode = "TASK-001",
            Status = "FeedbackPending",
            Message = "测试成功"
        });
    }
}
