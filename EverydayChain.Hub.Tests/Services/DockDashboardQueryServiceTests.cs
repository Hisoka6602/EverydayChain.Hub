using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Queries;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class DockDashboardQueryServiceTests
{
    [Fact]
    public async Task QueryAsync_ShouldApplyDockAggregationAndDockSevenExceptionRule()
    {
        var repository = new InMemoryBusinessTaskRepository();
        var service = new DockDashboardQueryService(repository);
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "A",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "A",
            TargetChuteCode = "7",
            Status = BusinessTaskStatus.Exception,
            IsException = true,
            CreatedTimeLocal = start.AddHours(1),
            UpdatedTimeLocal = start.AddHours(1)
        }, CancellationToken.None);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "B",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.FullCase,
            BusinessKey = "B",
            TargetChuteCode = "7",
            Status = BusinessTaskStatus.Dropped,
            ActualChuteCode = "8",
            CreatedTimeLocal = start.AddHours(2),
            UpdatedTimeLocal = start.AddHours(2)
        }, CancellationToken.None);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "C",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "C",
            TargetChuteCode = "8",
            Status = BusinessTaskStatus.Exception,
            IsException = true,
            CreatedTimeLocal = start.AddHours(3),
            UpdatedTimeLocal = start.AddHours(3)
        }, CancellationToken.None);

        var result = await service.QueryAsync(new DockDashboardQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end
        }, CancellationToken.None);

        Assert.Equal(2, result.DockSummaries.Count);
        Assert.Contains(result.DockSummaries, x => x.DockCode == "7" && x.ExceptionCount == 1 && x.RecirculatedCount == 0);
        Assert.Contains(result.DockSummaries, x => x.DockCode == "8" && x.ExceptionCount == 0 && x.RecirculatedCount == 2);
    }

    [Fact]
    public async Task QueryAsync_ShouldAutoSelectLatestScannedWave_WhenWaveCodeIsNotProvided()
    {
        var repository = new InMemoryBusinessTaskRepository();
        var service = new DockDashboardQueryService(repository);
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "A",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "A",
            WaveCode = "WAVE-001",
            TargetChuteCode = "7",
            Status = BusinessTaskStatus.Created,
            ScannedAtLocal = start.AddHours(1),
            CreatedTimeLocal = start.AddMinutes(1),
            UpdatedTimeLocal = start.AddMinutes(1)
        }, CancellationToken.None);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "B",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "B",
            WaveCode = "WAVE-002",
            TargetChuteCode = "8",
            Status = BusinessTaskStatus.Created,
            ScannedAtLocal = start.AddHours(2),
            CreatedTimeLocal = start.AddMinutes(2),
            UpdatedTimeLocal = start.AddMinutes(2)
        }, CancellationToken.None);

        var result = await service.QueryAsync(new DockDashboardQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end
        }, CancellationToken.None);

        Assert.Equal("WAVE-002", result.SelectedWaveCode);
        Assert.Single(result.DockSummaries);
        Assert.Equal("8", result.DockSummaries[0].DockCode);
    }
}

