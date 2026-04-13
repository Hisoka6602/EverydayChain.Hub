using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 格口服务测试替身。
/// </summary>
public sealed class StubChuteQueryService : IChuteQueryService {
    /// <summary>
    /// 返回固定测试结果。
    /// </summary>
    /// <param name="request">格口请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>固定结果。</returns>
    public Task<ChuteResolveApplicationResult> ExecuteAsync(ChuteResolveApplicationRequest request, CancellationToken cancellationToken) {
        _ = request;
        _ = cancellationToken;
        return Task.FromResult(new ChuteResolveApplicationResult {
            IsResolved = true,
            TaskCode = "TASK-001",
            ChuteCode = "CHUTE-01",
            Message = "测试成功"
        });
    }
}
