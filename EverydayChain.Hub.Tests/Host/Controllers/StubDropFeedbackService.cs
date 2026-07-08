using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 定义 StubDropFeedbackService 类型。
/// </summary>
public sealed class StubDropFeedbackService : IDropFeedbackService {
    /// <summary>
    /// 获取或设置 LastRequest。
    /// </summary>
    public DropFeedbackApplicationRequest? LastRequest { get; private set; }

    /// <summary>
    /// 执行 ExecuteAsync 方法。
    /// </summary>
    public Task<DropFeedbackApplicationResult> ExecuteAsync(DropFeedbackApplicationRequest request, CancellationToken cancellationToken) {
        // 步骤：执行 ExecuteAsync 方法的核心处理流程。
        LastRequest = request;
        _ = cancellationToken;
        return Task.FromResult(new DropFeedbackApplicationResult {
            IsAccepted = true,
            TaskCode = "TASK-001",
            Status = "FeedbackPending",
            Message = "测试成功"
        });
    }
}

