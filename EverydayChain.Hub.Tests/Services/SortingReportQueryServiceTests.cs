using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Queries;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 分拣报表查询服务测试。
/// </summary>
public sealed class SortingReportQueryServiceTests
{
    /// <summary>
    /// 报表查询应按码头输出聚合统计。
    /// </summary>
    [Fact]
    public async Task QueryAsync_ShouldAggregateRowsByDock()
    {
        var repository = new InMemoryBusinessTaskRepository();
        var service = new SortingReportQueryService(repository);
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "R1",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "R1",
            TargetChuteCode = "7",
            Status = BusinessTaskStatus.Dropped,
            CreatedTimeLocal = start.AddHours(1),
            UpdatedTimeLocal = start.AddHours(1)
        }, CancellationToken.None);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "R2",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.FullCase,
            BusinessKey = "R2",
            TargetChuteCode = "7",
            Status = BusinessTaskStatus.Created,
            IsException = true,
            CreatedTimeLocal = start.AddHours(2),
            UpdatedTimeLocal = start.AddHours(2)
        }, CancellationToken.None);

        var result = await service.QueryAsync(new SortingReportQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end,
            DockCode = "7"
        }, CancellationToken.None);

        Assert.Single(result.Rows);
        var row = result.Rows[0];
        Assert.Equal("7", row.DockCode);
        Assert.Equal(1, row.SplitTotalCount);
        Assert.Equal(1, row.FullCaseTotalCount);
        Assert.Equal(1, row.SplitSortedCount);
        Assert.Equal(0, row.FullCaseSortedCount);
        Assert.Equal(1, row.ExceptionCount);
    }

    /// <summary>
    /// CSV 导出应包含表头与数据行。
    /// </summary>
    [Fact]
    public async Task ExportCsvAsync_ShouldContainHeaderAndRows()
    {
        var repository = new InMemoryBusinessTaskRepository();
        var service = new SortingReportQueryService(repository);
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "R3",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "R3",
            TargetChuteCode = "9",
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = start.AddHours(1),
            UpdatedTimeLocal = start.AddHours(1)
        }, CancellationToken.None);

        var csv = await service.ExportCsvAsync(new SortingReportQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end
        }, CancellationToken.None);

        Assert.Contains("码头号,拆零总数", csv);
        Assert.Contains("9,1,0", csv);
    }
}
