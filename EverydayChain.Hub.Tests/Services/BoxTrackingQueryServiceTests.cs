using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Queries;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Tests.Services;

public sealed class BoxTrackingQueryServiceTests
{
    [Fact]
    public async Task QueryAsync_ShouldReturnScanTraceRows()
    {
        var businessTaskRepository = new InMemoryBusinessTaskRepository();
        var scanLogRepository = new InMemoryScanLogRepository();
        var service = new BoxTrackingQueryService(scanLogRepository, businessTaskRepository);
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);

        var task = new BusinessTaskEntity
        {
            TaskCode = "TASK-001",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "KEY-001",
            Barcode = "BOX-001",
            WaveCode = "WAVE-001",
            OrderId = "ORDER-001",
            StoreId = "STORE-001",
            StoreName = "Store One",
            ProductCode = "SKU-001",
            PickLocation = "LOC-001",
            TargetChuteCode = "B-07",
            Status = BusinessTaskStatus.Scanned,
            CreatedTimeLocal = start.AddMinutes(1),
            UpdatedTimeLocal = start.AddMinutes(1)
        };
        await businessTaskRepository.SaveAsync(task, CancellationToken.None);
        await scanLogRepository.SaveAsync(new ScanLogEntity
        {
            BusinessTaskId = task.Id,
            TaskCode = task.TaskCode,
            Barcode = "BOX-001",
            DeviceCode = "SCN-01",
            IsMatched = true,
            ScanTimeLocal = start.AddHours(1),
            CreatedTimeLocal = start.AddHours(1)
        }, CancellationToken.None);

        var result = await service.QueryAsync(new BoxTrackingQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end
        }, CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("BOX-001", item.BoxId);
        Assert.Equal("TASK-001", item.TaskCode);
        Assert.Equal("WAVE-001", item.WaveCode);
        Assert.Equal("ORDER-001", item.OrderId);
        Assert.Equal("STORE-001", item.StoreId);
        Assert.Equal("Store One", item.StoreName);
        Assert.Equal("SKU-001", item.ProductCode);
        Assert.Equal("LOC-001", item.PickLocation);
        Assert.Equal("SCN-01", item.Scanner);
        Assert.Equal("B-07", item.Chute);
        Assert.Equal("Scanned", item.Status);
    }

    [Fact]
    public async Task QueryAsync_ShouldFilterByChuteCode()
    {
        var businessTaskRepository = new InMemoryBusinessTaskRepository();
        var scanLogRepository = new InMemoryScanLogRepository();
        var service = new BoxTrackingQueryService(scanLogRepository, businessTaskRepository);
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);

        var task = new BusinessTaskEntity
        {
            TaskCode = "TASK-002",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "KEY-002",
            Barcode = "BOX-002",
            TargetChuteCode = "C-03",
            Status = BusinessTaskStatus.Scanned,
            CreatedTimeLocal = start.AddMinutes(1),
            UpdatedTimeLocal = start.AddMinutes(1)
        };
        await businessTaskRepository.SaveAsync(task, CancellationToken.None);
        await scanLogRepository.SaveAsync(new ScanLogEntity
        {
            BusinessTaskId = task.Id,
            TaskCode = task.TaskCode,
            Barcode = "BOX-002",
            DeviceCode = "SCN-02",
            IsMatched = true,
            ScanTimeLocal = start.AddHours(2),
            CreatedTimeLocal = start.AddHours(2)
        }, CancellationToken.None);

        var result = await service.QueryAsync(new BoxTrackingQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end,
            ChuteCode = "B-07"
        }, CancellationToken.None);

        Assert.Empty(result.Items);
    }
}
