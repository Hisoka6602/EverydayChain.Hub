using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义 IScanIngressService 类型。
/// </summary>
public interface IScanIngressService {
    /// <summary>
    /// 执行 ExecuteAsync 方法。
    /// </summary>
    Task<ScanUploadApplicationResult> ExecuteAsync(ScanUploadApplicationRequest request, CancellationToken cancellationToken);
}

