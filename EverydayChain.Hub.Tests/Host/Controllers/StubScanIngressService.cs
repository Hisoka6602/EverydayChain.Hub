using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 扫描服务测试替身。
/// </summary>
public sealed class StubScanIngressService : IScanIngressService {
    /// <summary>
    /// 返回固定测试结果。
    /// </summary>
    /// <param name="request">扫描请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>固定结果。</returns>
    public Task<ScanUploadApplicationResult> ExecuteAsync(ScanUploadApplicationRequest request, CancellationToken cancellationToken) {
        _ = request;
        _ = cancellationToken;
        return Task.FromResult(new ScanUploadApplicationResult {
            IsAccepted = true,
            TaskCode = "TASK-001",
            Message = "测试成功"
        });
    }
}
