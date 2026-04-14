using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 扫描服务测试替身。
/// </summary>
public sealed class StubScanIngressService : IScanIngressService {
    /// <summary>
    /// 最近一次接收的请求。
    /// </summary>
    public ScanUploadApplicationRequest? LastRequest { get; private set; }

    /// <summary>
    /// 已接收的全部请求。
    /// </summary>
    public List<ScanUploadApplicationRequest> Requests { get; } = [];

    /// <summary>
    /// 测试返回结果，用于在不同测试场景下配置控制器收到的应用层响应。
    /// </summary>
    public ScanUploadApplicationResult Result { get; set; } = new ScanUploadApplicationResult {
        IsAccepted = true,
        TaskCode = "TASK-001",
        BarcodeType = "Split",
        FailureReason = string.Empty,
        Message = "测试成功"
    };

    /// <summary>
    /// 返回固定测试结果。
    /// </summary>
    /// <param name="request">扫描请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>固定结果。</returns>
    public Task<ScanUploadApplicationResult> ExecuteAsync(ScanUploadApplicationRequest request, CancellationToken cancellationToken) {
        LastRequest = request;
        Requests.Add(request);
        _ = cancellationToken;
        return Task.FromResult(Result);
    }
}
