using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class StubDropFeedbackService : IDropFeedbackService {
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DropFeedbackApplicationRequest? LastRequest { get; private set; }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public Task<DropFeedbackApplicationResult> ExecuteAsync(DropFeedbackApplicationRequest request, CancellationToken cancellationToken) {
        // 步骤：按既定流程执行当前方法逻辑。
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

