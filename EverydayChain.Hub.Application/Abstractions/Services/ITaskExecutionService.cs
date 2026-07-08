using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义 ITaskExecutionService 类型。
/// </summary>
public interface ITaskExecutionService
{
    /// <summary>
    /// 执行 MarkScannedAsync 方法。
    /// </summary>
    Task<TaskExecutionResult> MarkScannedAsync(ScanUploadApplicationRequest request, CancellationToken ct);
}

