using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Queries;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Services;

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
}
