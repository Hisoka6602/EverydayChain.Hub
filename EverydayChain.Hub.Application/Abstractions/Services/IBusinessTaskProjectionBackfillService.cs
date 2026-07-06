using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IBusinessTaskProjectionBackfillService
{
    Task<BusinessTaskProjectionBackfillPreviewResult> PreviewAsync(
        BusinessTaskProjectionBackfillPreviewCommand command,
        CancellationToken cancellationToken);

    Task<BusinessTaskProjectionBackfillResult> ExecuteAsync(
        BusinessTaskProjectionBackfillCommand command,
        CancellationToken cancellationToken);
}

