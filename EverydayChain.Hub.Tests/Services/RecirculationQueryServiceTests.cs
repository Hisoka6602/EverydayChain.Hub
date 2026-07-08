using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Queries;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 RecirculationQueryServiceTests 类型。
/// </summary>
public sealed class RecirculationQueryServiceTests
{
    [Fact]
    public async Task QuerySummaryAsync_ShouldGroupByChuteAndWave()
    {
        var repository = new InMemoryBusinessTaskRepository();
        var service = new RecirculationQueryService(repository);
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-001",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "KEY-001",
            WaveCode = "WAVE-001",
            ActualChuteCode = "8",
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = start.AddMinutes(1),
            UpdatedTimeLocal = start.AddMinutes(1)
        }, CancellationToken.None);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-002",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "KEY-002",
            WaveCode = "WAVE-001",
            ActualChuteCode = "8",
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = start.AddMinutes(2),
            UpdatedTimeLocal = start.AddMinutes(2)
        }, CancellationToken.None);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-003",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.FullCase,
            BusinessKey = "KEY-003",
            WaveCode = "WAVE-002",
            ActualChuteCode = "9",
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = start.AddMinutes(3),
            UpdatedTimeLocal = start.AddMinutes(3)
        }, CancellationToken.None);

        var result = await service.QuerySummaryAsync(new RecirculationSummaryQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end,
            SortOrder = "Most"
        }, CancellationToken.None);

        Assert.Equal(2, result.Rows.Count);
        Assert.Equal("8", result.Rows[0].ChuteCode);
        Assert.Equal("WAVE-001", result.Rows[0].WaveCode);
        Assert.Equal(2, result.Rows[0].RecirculatedCount);
    }
}

