using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义 IChuteQueryService 类型。
/// </summary>
public interface IChuteQueryService {
    /// <summary>
    /// 执行 ExecuteAsync 方法。
    /// </summary>
    Task<ChuteResolveApplicationResult> ExecuteAsync(ChuteResolveApplicationRequest request, CancellationToken cancellationToken);
}

