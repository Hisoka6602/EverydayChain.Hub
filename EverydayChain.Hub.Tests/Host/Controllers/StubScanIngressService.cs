using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 定义 StubScanIngressService 类型。
/// </summary>
public sealed class StubScanIngressService : IScanIngressService {
    /// <summary>
    /// 获取或设置 LastRequest。
    /// </summary>
    public ScanUploadApplicationRequest? LastRequest { get; private set; }

    /// <summary>
    /// 获取或设置 Requests。
    /// </summary>
    public List<ScanUploadApplicationRequest> Requests { get; } = [];

    /// <summary>
    /// 获取或设置 Result。
    /// </summary>
    public ScanUploadApplicationResult Result { get; set; } = new ScanUploadApplicationResult {
        IsAccepted = true,
        TaskCode = "TASK-001",
        BarcodeType = "Split",
        FailureReason = string.Empty,
        Message = "测试成功"
    };

    /// <summary>
    /// 执行 ExecuteAsync 方法。
    /// </summary>
    public Task<ScanUploadApplicationResult> ExecuteAsync(ScanUploadApplicationRequest request, CancellationToken cancellationToken) {
        // 步骤：执行 ExecuteAsync 方法的核心处理流程。
        LastRequest = request;
        Requests.Add(request);
        _ = cancellationToken;
        return Task.FromResult(Result);
    }
}

