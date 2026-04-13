using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 任务执行服务抽象，负责在匹配成功后推进任务状态并持久化。
/// </summary>
public interface ITaskExecutionService
{
    /// <summary>
    /// 将业务任务标记为已扫描，并持久化扫描信息。
    /// </summary>
    /// <param name="request">扫描上传应用层请求，包含扫描时间、设备编码等信息。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>任务执行结果。</returns>
    Task<TaskExecutionResult> MarkScannedAsync(ScanUploadApplicationRequest request, CancellationToken ct);
}
