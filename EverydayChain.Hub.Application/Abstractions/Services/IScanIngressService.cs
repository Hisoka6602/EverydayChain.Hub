using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IScanIngressService {
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<ScanUploadApplicationResult> ExecuteAsync(ScanUploadApplicationRequest request, CancellationToken cancellationToken);
}

