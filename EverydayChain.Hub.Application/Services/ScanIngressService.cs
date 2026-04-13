using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 扫描上传应用服务骨架实现。
/// </summary>
public sealed class ScanIngressService : IScanIngressService {
    /// <summary>
    /// 处理扫描上传请求并返回标准化结果。
    /// </summary>
    /// <param name="request">扫描上传请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>处理结果。</returns>
    public Task<ScanUploadApplicationResult> ExecuteAsync(ScanUploadApplicationRequest request, CancellationToken cancellationToken) {
        _ = cancellationToken;
        var taskCodeCandidate = string.IsNullOrWhiteSpace(request.DeviceCode)
            ? string.Empty
            : $"{request.DeviceCode.Trim()}-{request.ScanTimeLocal:yyyyMMddHHmmss}";
        var normalizedTaskCode = string.IsNullOrWhiteSpace(taskCodeCandidate)
            ? $"TASK-{request.Barcode.Trim()}"
            : taskCodeCandidate;
        var result = new ScanUploadApplicationResult {
            IsAccepted = true,
            TaskCode = normalizedTaskCode,
            Message = "扫描请求已受理，后续阶段将接入匹配与状态推进链路。"
        };
        return Task.FromResult(result);
    }
}
