using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 请求格口应用服务抽象。
/// </summary>
public interface IChuteQueryService {
    /// <summary>
    /// 解析目标格口。
    /// </summary>
    /// <param name="request">请求参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>格口解析结果。</returns>
    Task<ChuteResolveApplicationResult> ExecuteAsync(ChuteResolveApplicationRequest request, CancellationToken cancellationToken);
}
