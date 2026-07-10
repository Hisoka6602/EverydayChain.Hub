using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 InMemoryScanLogRepository 类型。
/// </summary>
internal sealed class InMemoryScanLogRepository : IScanLogRepository
{
    /// <summary>
    /// 获取或设置 QueryRangeCallCount。
    /// </summary>
    public int QueryRangeCallCount { get; private set; }

    /// <summary>
    /// 获取或设置 QueryPageCallCount。
    /// </summary>
    public int QueryPageCallCount { get; private set; }

    /// <summary>
    /// 获取或设置 Logs。
    /// </summary>
    public List<ScanLogEntity> Logs { get; } = [];

    /// <summary>
    /// 存储 _nextId 字段。
    /// </summary>
    private long _nextId = 1;

    public Task SaveAsync(ScanLogEntity entity, CancellationToken ct)
    {
        entity.Id = _nextId++;
        Logs.Add(entity);
        return Task.CompletedTask;
    }

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

    /// <summary>
    /// 执行 QueryRangeAsync 方法。
    /// </summary>
    public Task<IReadOnlyList<ScanLogEntity>> QueryRangeAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? barcode,
        string? deviceCode,
        CancellationToken ct)
    {
        QueryRangeCallCount++;
        // 步骤：执行 QueryRangeAsync 方法的核心处理流程。
        var normalizedBarcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();
        var normalizedDeviceCode = string.IsNullOrWhiteSpace(deviceCode) ? null : deviceCode.Trim();
        IReadOnlyList<ScanLogEntity> items = Logs
            .Where(x => x.ScanTimeLocal >= startTimeLocal && x.ScanTimeLocal < endTimeLocal)
            .Where(x => normalizedBarcode is null || string.Equals(x.Barcode, normalizedBarcode, StringComparison.OrdinalIgnoreCase))
            .Where(x => normalizedDeviceCode is null || string.Equals(x.DeviceCode, normalizedDeviceCode, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.ScanTimeLocal)
            .ThenByDescending(x => x.Id)
            .ToList();
        return Task.FromResult(items);
    }

    /// <summary>
    /// 执行 QueryPageAsync 方法。
    /// </summary>
    public Task<(int TotalCount, IReadOnlyList<ScanLogEntity> Items)> QueryPageAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? barcode,
        string? deviceCode,
        int skip,
        int take,
        CancellationToken ct)
    {
        QueryPageCallCount++;
        // 步骤：执行 QueryPageAsync 方法的核心处理流程。
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

