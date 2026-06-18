using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 扫描日志仓储内存替身，用于单元测试。
/// </summary>
internal sealed class InMemoryScanLogRepository : IScanLogRepository
{
    /// <summary>内存日志存储。</summary>
    public List<ScanLogEntity> Logs { get; } = [];

    /// <summary>自增 Id 计数器。</summary>
    private long _nextId = 1;

    /// <inheritdoc/>
    public Task SaveAsync(ScanLogEntity entity, CancellationToken ct)
    {
        entity.Id = _nextId++;
        Logs.Add(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<ScanLogRecognitionAggregate> AggregateRecognitionAsync(DateTime startTimeLocal, DateTime endTimeLocal, CancellationToken ct)
    {
        var aggregate = new ScanLogRecognitionAggregate();
        if (endTimeLocal <= startTimeLocal)
        {
            return Task.FromResult(aggregate);
        }

        var filtered = Logs.Where(x => x.ScanTimeLocal >= startTimeLocal && x.ScanTimeLocal < endTimeLocal).ToList();
        aggregate.TotalScanCount = filtered.Count;
        aggregate.MatchedScanCount = filtered.Count(x => x.IsMatched);
        return Task.FromResult(aggregate);
    }

    public Task<(int TotalCount, IReadOnlyList<ScanLogEntity> Items)> QueryPageAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? barcode,
        string? deviceCode,
        int skip,
        int take,
        CancellationToken ct)
    {
        var normalizedBarcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();
        var normalizedDeviceCode = string.IsNullOrWhiteSpace(deviceCode) ? null : deviceCode.Trim();
        var ordered = Logs
            .Where(x => x.ScanTimeLocal >= startTimeLocal && x.ScanTimeLocal < endTimeLocal)
            .Where(x => normalizedBarcode is null || string.Equals(x.Barcode, normalizedBarcode, StringComparison.OrdinalIgnoreCase))
            .Where(x => normalizedDeviceCode is null || string.Equals(x.DeviceCode, normalizedDeviceCode, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.ScanTimeLocal)
            .ThenByDescending(x => x.Id)
            .ToList();
        return Task.FromResult(((int)ordered.Count, (IReadOnlyList<ScanLogEntity>)ordered.Skip(skip).Take(take).ToList()));
    }
}
