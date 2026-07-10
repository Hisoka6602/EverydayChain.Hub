using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Queries;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 WaveQueryServiceTests 类型。
/// </summary>
public sealed class WaveQueryServiceTests
{
    [Fact]
    public async Task QueryCurrentAsync_ShouldReturnLatestScannedWave_WhenMatchedTaskExists()
    {
        var repository = new InMemoryBusinessTaskRepository();
        var service = new WaveQueryService(repository, NullLogger<WaveQueryService>.Instance);
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-001",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "KEY-001",
            Barcode = "BC-001",
            WaveCode = "WAVE-001",
            WaveRemark = "Remark 1",
            Status = BusinessTaskStatus.Created,
            ScannedAtLocal = start.AddHours(1),
            CreatedTimeLocal = start.AddMinutes(1),
            UpdatedTimeLocal = start.AddMinutes(1)
        }, CancellationToken.None);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-002",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "KEY-002",
            Barcode = "BC-002",
            WaveCode = "WAVE-002",
            WaveRemark = "Remark 2",
            Status = BusinessTaskStatus.Created,
            ScannedAtLocal = start.AddHours(3),
            CreatedTimeLocal = start.AddMinutes(2),
            UpdatedTimeLocal = start.AddMinutes(2)
        }, CancellationToken.None);

        var result = await service.QueryCurrentAsync(new CurrentWaveQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end
        }, CancellationToken.None);

        Assert.Equal("WAVE-002", result.WaveCode);
        Assert.Equal("BC-002", result.Barcode);
        Assert.Equal(start.AddHours(3), result.ScanTimeLocal);
    }

    [Fact]
    public async Task QueryListAsync_ShouldBuildWaveDetailRows()
    {
        var repository = new InMemoryBusinessTaskRepository();
        var service = new WaveQueryService(repository, NullLogger<WaveQueryService>.Instance);
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-001",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "KEY-001",
            WaveCode = "WAVE-001",
            WaveRemark = "Remark 1",
            ActualChuteCode = "8",
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = start.AddMinutes(1),
            UpdatedTimeLocal = start.AddMinutes(1)
        }, CancellationToken.None);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-002",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.FullCase,
            BusinessKey = "KEY-002",
            WaveCode = "WAVE-001",
            WaveRemark = "Remark 1",
            Status = BusinessTaskStatus.Dropped,
            CreatedTimeLocal = start.AddMinutes(2),
            UpdatedTimeLocal = start.AddMinutes(2)
        }, CancellationToken.None);

        var result = await service.QueryListAsync(new WaveListQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end
        }, CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal("WAVE-001", item.WaveCode);
        Assert.Equal("Remark 1", item.WaveRemark);
        Assert.Equal(2, item.PackageTotal);
        Assert.Equal(1, item.SplitTotal);
        Assert.Equal(1, item.FullCaseTotal);
        Assert.Equal(start.AddMinutes(1), item.CreatedTimeLocal);
        Assert.Equal("Sorting", item.Status);
    }

    [Fact]
    public async Task QueryDetailsAsync_ShouldReturnTaskLevelRows_WithExtendedTraceFields()
    {
        var repository = new InMemoryBusinessTaskRepository();
        var service = new WaveQueryService(repository, NullLogger<WaveQueryService>.Instance);
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-001",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.FullCase,
            BusinessKey = "KEY-001",
            Barcode = "BC-001",
            WaveCode = "WAVE-001",
            WaveRemark = "Remark 1",
            OrderId = "ORDER-001",
            StoreId = "STORE-001",
            StoreName = "Store 1",
            ProductCode = "SKU-001",
            PickLocation = "A-01-01",
            ActualChuteCode = "8",
            Status = BusinessTaskStatus.Scanned,
            ScannedAtLocal = start.AddHours(2),
            CreatedTimeLocal = start.AddMinutes(1),
            UpdatedTimeLocal = start.AddHours(2).AddMinutes(1)
        }, CancellationToken.None);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-002",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "KEY-002",
            Barcode = "BC-002",
            WaveCode = "WAVE-001",
            WaveRemark = "Remark 2",
            WorkingArea = "2",
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = start.AddMinutes(2),
            UpdatedTimeLocal = start.AddMinutes(2)
        }, CancellationToken.None);

        var result = await service.QueryDetailsAsync(new WaveDetailQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end,
            WaveCode = "WAVE-001"
        }, CancellationToken.None);

        Assert.Equal("WAVE-001", result.WaveCode);
        Assert.Equal("Remark 1", result.WaveRemark);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("TASK-001", result.Items[0].TaskCode);
        Assert.Equal("ORDER-001", result.Items[0].OrderId);
        Assert.Equal("STORE-001", result.Items[0].StoreId);
        Assert.Equal("Store 1", result.Items[0].StoreName);
        Assert.Equal("SKU-001", result.Items[0].ProductCode);
        Assert.Equal("A-01-01", result.Items[0].PickLocation);
        Assert.Equal("8", result.Items[0].ChuteCode);
        Assert.True(result.Items[0].IsRecirculated);
        Assert.Equal("FullCase", result.Items[0].SourceType);
        Assert.Equal("TASK-002", result.Items[1].TaskCode);
    }

    [Fact]
    public async Task QueryDetailsAsync_ShouldReuseCacheWithinSameTimeBucket()
    {
        var repository = new InMemoryBusinessTaskRepository();
        var service = new WaveQueryService(
            repository,
            NullLogger<WaveQueryService>.Instance,
            new MemoryCache(new MemoryCacheOptions()),
            new QueryCacheOptions
            {
                Enabled = true,
                AggregateTimeBucketSeconds = 30,
                WaveDetailSeconds = 10
            });
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-CACHE-001",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.FullCase,
            BusinessKey = "KEY-CACHE-001",
            Barcode = "BC-CACHE-001",
            WaveCode = "WAVE-CACHE-001",
            Status = BusinessTaskStatus.Scanned,
            CreatedTimeLocal = start.AddMinutes(1),
            UpdatedTimeLocal = start.AddMinutes(1)
        }, CancellationToken.None);

        _ = await service.QueryDetailsAsync(new WaveDetailQueryRequest
        {
            StartTimeLocal = start.AddSeconds(5),
            EndTimeLocal = end.AddSeconds(5),
            WaveCode = "WAVE-CACHE-001"
        }, CancellationToken.None);

        _ = await service.QueryDetailsAsync(new WaveDetailQueryRequest
        {
            StartTimeLocal = start.AddSeconds(20),
            EndTimeLocal = end.AddSeconds(20),
            WaveCode = "WAVE-CACHE-001"
        }, CancellationToken.None);

        Assert.Equal(1, repository.FindByWaveCodeAndCreatedTimeRangeCallCount);
    }

    [Fact]
    public async Task QueryZonesAsync_ShouldSupportAlphanumericWorkingAreaAliases()
    {
        var repository = new InMemoryBusinessTaskRepository();
        var service = new WaveQueryService(repository, NullLogger<WaveQueryService>.Instance);
        var start = DateTime.SpecifyKind(new DateTime(2026, 7, 9, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);

        await repository.SaveAsync(CreateWaveTask("TASK-001", "N26071500", BusinessTaskSourceType.Split, "1", BusinessTaskStatus.Created, start.AddMinutes(1)), CancellationToken.None);
        await repository.SaveAsync(CreateWaveTask("TASK-002", "N26071500", BusinessTaskSourceType.Split, "YA2", BusinessTaskStatus.Dropped, start.AddMinutes(2)), CancellationToken.None);
        await repository.SaveAsync(CreateWaveTask("TASK-003", "N26071500", BusinessTaskSourceType.Split, "YB1", BusinessTaskStatus.Created, start.AddMinutes(3)), CancellationToken.None);
        await repository.SaveAsync(CreateWaveTask("TASK-004", "N26071500", BusinessTaskSourceType.Split, "YB2", BusinessTaskStatus.Exception, start.AddMinutes(4)), CancellationToken.None);
        await repository.SaveAsync(CreateWaveTask("TASK-005", "N26071500", BusinessTaskSourceType.FullCase, null, BusinessTaskStatus.Dropped, start.AddMinutes(5)), CancellationToken.None);

        var result = await service.QueryZonesAsync(new WaveZoneQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end,
            WaveCode = "N26071500"
        }, CancellationToken.None);

        Assert.NotNull(result);
        var zoneMap = result!.Zones.ToDictionary(item => item.ZoneCode, StringComparer.Ordinal);
        Assert.Equal(1, zoneMap["SplitZone1"].TotalCount);
        Assert.Equal(1, zoneMap["SplitZone1"].UnsortedCount);
        Assert.Equal(1, zoneMap["SplitZone2"].TotalCount);
        Assert.Equal(0, zoneMap["SplitZone2"].UnsortedCount);
        Assert.Equal(1, zoneMap["SplitZone3"].TotalCount);
        Assert.Equal(1, zoneMap["SplitZone3"].UnsortedCount);
        Assert.Equal(1, zoneMap["SplitZone4"].TotalCount);
        Assert.Equal(1, zoneMap["SplitZone4"].ExceptionCount);
        Assert.Equal(1, zoneMap["FullCase"].TotalCount);
        Assert.Equal(0, zoneMap["FullCase"].UnsortedCount);
    }

    private static BusinessTaskEntity CreateWaveTask(
        string taskCode,
        string waveCode,
        BusinessTaskSourceType sourceType,
        string? workingArea,
        BusinessTaskStatus status,
        DateTime createdTimeLocal)
    {
        return new BusinessTaskEntity
        {
            TaskCode = taskCode,
            SourceTableCode = "SRC",
            SourceType = sourceType,
            BusinessKey = taskCode,
            WaveCode = waveCode,
            WaveRemark = "Wave Remark",
            WorkingArea = workingArea,
            Status = status,
            CreatedTimeLocal = createdTimeLocal,
            UpdatedTimeLocal = createdTimeLocal
        };
    }
}

