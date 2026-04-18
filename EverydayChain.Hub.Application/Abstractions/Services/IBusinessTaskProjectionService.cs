using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 业务任务投影服务抽象，负责将状态驱动读取行映射为业务任务实体。
/// </summary>
public interface IBusinessTaskProjectionService
{
    /// <summary>
    /// 执行业务任务投影。
    /// </summary>
    /// <param name="request">投影请求。</param>
    /// <returns>投影结果。</returns>
    BusinessTaskProjectionResult Project(BusinessTaskProjectionRequest request);
}
