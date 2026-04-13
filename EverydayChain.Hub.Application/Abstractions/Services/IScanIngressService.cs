using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 扫描上传应用服务抽象。
/// </summary>
public interface IScanIngressService {
    /// <summary>
    /// 处理扫描上传请求。
    /// </summary>
    /// <param name="request">扫描上传请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>处理结果。</returns>
    Task<ScanUploadApplicationResult> ExecuteAsync(ScanUploadApplicationRequest request, CancellationToken cancellationToken);
}
