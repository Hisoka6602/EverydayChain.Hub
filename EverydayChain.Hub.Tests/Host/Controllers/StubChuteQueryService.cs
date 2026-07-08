using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 定义 StubChuteQueryService 类型。
/// </summary>
public sealed class StubChuteQueryService : IChuteQueryService {
    /// <summary>
    /// 获取或设置 LastRequest。
    /// </summary>
    public ChuteResolveApplicationRequest? LastRequest { get; private set; }

    /// <summary>
    /// 执行 ExecuteAsync 方法。
    /// </summary>
    public Task<ChuteResolveApplicationResult> ExecuteAsync(ChuteResolveApplicationRequest request, CancellationToken cancellationToken) {
        // 步骤：执行 ExecuteAsync 方法的核心处理流程。
        LastRequest = request;
        _ = cancellationToken;
        return Task.FromResult(new ChuteResolveApplicationResult {
            IsResolved = true,
            TaskCode = "TASK-001",
            ChuteCode = "CHUTE-01",
            Message = "测试成功"
        });
    }
}

