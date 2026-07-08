using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义 IBusinessTaskProjectionService 类型。
/// </summary>
public interface IBusinessTaskProjectionService
{
    /// <summary>
    /// 执行 Project 方法。
    /// </summary>
    BusinessTaskProjectionResult Project(BusinessTaskProjectionRequest request);
}

