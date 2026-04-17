using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Queries;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 码头看板查询服务测试。
/// </summary>
public sealed class DockDashboardQueryServiceTests
{
    /// <summary>
    /// 查询结果应满足码头聚合与 7 号码头异常规则。
    /// </summary>
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
            IsRecirculated = true,
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
        Assert.Contains(result.DockSummaries, x => x.DockCode == "7" && x.ExceptionCount == 1 && x.RecirculatedCount == 1);
        Assert.Contains(result.DockSummaries, x => x.DockCode == "8" && x.ExceptionCount == 0);
    }
}
