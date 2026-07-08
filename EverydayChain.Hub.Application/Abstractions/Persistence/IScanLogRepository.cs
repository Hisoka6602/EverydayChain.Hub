using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义 IScanLogRepository 类型。
/// </summary>
public interface IScanLogRepository
{
    /// <summary>
    /// 执行 SaveAsync 方法。
    /// </summary>
    Task SaveAsync(ScanLogEntity entity, CancellationToken ct);

    /// <summary>
    /// 执行 AggregateRecognitionAsync 方法。
    /// </summary>
    Task<ScanLogRecognitionAggregate> AggregateRecognitionAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct);

    Task<IReadOnlyList<ScanLogEntity>> QueryRangeAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? barcode,
        string? deviceCode,
        CancellationToken ct);

    Task<(int TotalCount, IReadOnlyList<ScanLogEntity> Items)> QueryPageAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? barcode,
        string? deviceCode,
        int skip,
        int take,
        CancellationToken ct);
}

