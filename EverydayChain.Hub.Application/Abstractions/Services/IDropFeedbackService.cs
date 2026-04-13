using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 落格回传应用服务抽象。
/// </summary>
public interface IDropFeedbackService {
    /// <summary>
    /// 处理落格回传。
    /// </summary>
    /// <param name="request">落格回传请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>处理结果。</returns>
    Task<DropFeedbackApplicationResult> ExecuteAsync(DropFeedbackApplicationRequest request, CancellationToken cancellationToken);
}
