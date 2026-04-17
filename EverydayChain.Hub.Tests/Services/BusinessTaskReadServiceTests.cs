using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Queries;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 业务任务查询服务测试。
/// </summary>
public sealed class BusinessTaskReadServiceTests
{
    /// <summary>
    /// 业务任务查询应支持筛选与分页。
    /// </summary>
    [Fact]
    public async Task QueryTasksAsync_ShouldApplyFiltersAndPaging()
    {
        var repository = new InMemoryBusinessTaskRepository();
        var service = new BusinessTaskReadService(repository);
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "Q1",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "Q1",
            WaveCode = "W1",
            Barcode = "B1",
            TargetChuteCode = "7",
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = start.AddHours(1),
            UpdatedTimeLocal = start.AddHours(1)
        }, CancellationToken.None);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "Q2",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.FullCase,
            BusinessKey = "Q2",
            WaveCode = "W2",
            Barcode = "B2",
            TargetChuteCode = "8",
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = start.AddHours(2),
            UpdatedTimeLocal = start.AddHours(2)
        }, CancellationToken.None);

        var result = await service.QueryTasksAsync(new BusinessTaskQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end,
            WaveCode = "W1",
            PageNumber = 1,
            PageSize = 10
        }, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Q1", result.Items[0].TaskCode);
    }

    /// <summary>
    /// 异常件与回流查询应按对应状态筛选。
    /// </summary>
    [Fact]
    public async Task QueryExceptionsAndRecirculations_ShouldReturnMatchedRows()
    {
        var repository = new InMemoryBusinessTaskRepository();
        var service = new BusinessTaskReadService(repository);
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "E1",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "E1",
            TargetChuteCode = "7",
            Status = BusinessTaskStatus.Exception,
            IsException = true,
            IsRecirculated = true,
            CreatedTimeLocal = start.AddHours(1),
            UpdatedTimeLocal = start.AddHours(1)
        }, CancellationToken.None);

        var exceptionResult = await service.QueryExceptionsAsync(new BusinessTaskQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end,
            PageNumber = 1,
            PageSize = 10
        }, CancellationToken.None);

        var recirculationResult = await service.QueryRecirculationsAsync(new BusinessTaskQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end,
            PageNumber = 1,
            PageSize = 10
        }, CancellationToken.None);

        Assert.Single(exceptionResult.Items);
        Assert.Single(recirculationResult.Items);
        Assert.Equal("E1", exceptionResult.Items[0].TaskCode);
        Assert.Equal("E1", recirculationResult.Items[0].TaskCode);
    }
}
