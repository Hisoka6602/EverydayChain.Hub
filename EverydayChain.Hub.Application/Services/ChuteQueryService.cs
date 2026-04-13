using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 请求格口应用服务骨架实现。
/// </summary>
public sealed class ChuteQueryService : IChuteQueryService {
    /// <summary>
    /// 按请求参数返回目标格口骨架结果。
    /// </summary>
    /// <param name="request">请求参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>格口解析结果。</returns>
    public Task<ChuteResolveApplicationResult> ExecuteAsync(ChuteResolveApplicationRequest request, CancellationToken cancellationToken) {
        _ = cancellationToken;
        var normalizedTaskCode = string.IsNullOrWhiteSpace(request.TaskCode)
            ? $"TASK-{request.Barcode}"
            : request.TaskCode;
        var result = new ChuteResolveApplicationResult {
            IsResolved = true,
            TaskCode = normalizedTaskCode,
            ChuteCode = "CHUTE-PLACEHOLDER",
            Message = "格口请求已受理，后续阶段将接入真实格口规则。"
        };

        return Task.FromResult(result);
    }
}
