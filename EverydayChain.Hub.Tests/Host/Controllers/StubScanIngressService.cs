using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class StubScanIngressService : IScanIngressService {
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public ScanUploadApplicationRequest? LastRequest { get; private set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public List<ScanUploadApplicationRequest> Requests { get; } = [];

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public ScanUploadApplicationResult Result { get; set; } = new ScanUploadApplicationResult {
        IsAccepted = true,
        TaskCode = "TASK-001",
        BarcodeType = "Split",
        FailureReason = string.Empty,
        Message = "测试成功"
    };

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public Task<ScanUploadApplicationResult> ExecuteAsync(ScanUploadApplicationRequest request, CancellationToken cancellationToken) {
        // 步骤：按既定流程执行当前方法逻辑。
        LastRequest = request;
        Requests.Add(request);
        _ = cancellationToken;
        return Task.FromResult(Result);
    }
}

