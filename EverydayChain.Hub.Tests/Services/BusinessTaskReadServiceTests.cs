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
            ActualChuteCode = "8",
            Status = BusinessTaskStatus.Exception,
            IsException = true,
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

    /// <summary>
    /// 回流查询应仅按归并码头编码口径筛选，不受 IsRecirculated 领域状态字段影响。
    /// </summary>
    [Fact]
    public async Task QueryRecirculationsAsync_ShouldUseResolvedDockCodeRule()
    {
        var repository = new InMemoryBusinessTaskRepository();
        var service = new BusinessTaskReadService(repository);
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "R1",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "R1",
            TargetChuteCode = "7",
            ActualChuteCode = "7",
            IsRecirculated = true,
            Status = BusinessTaskStatus.Scanned,
            CreatedTimeLocal = start.AddHours(1),
            UpdatedTimeLocal = start.AddHours(1)
        }, CancellationToken.None);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "R2",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "R2",
            TargetChuteCode = "7",
            ActualChuteCode = "8",
            IsRecirculated = false,
            Status = BusinessTaskStatus.Scanned,
            CreatedTimeLocal = start.AddHours(2),
            UpdatedTimeLocal = start.AddHours(2)
        }, CancellationToken.None);

        var result = await service.QueryRecirculationsAsync(new BusinessTaskQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end,
            PageNumber = 1,
            PageSize = 10
        }, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("R2", result.Items[0].TaskCode);
    }

    /// <summary>
    /// 回流筛选应按 Trim 后的归并码头编码判定。
    /// </summary>
    [Fact]
    public async Task QueryRecirculationsAsync_ShouldUseTrimmedResolvedDockCode()
    {
        var repository = new InMemoryBusinessTaskRepository();
        var service = new BusinessTaskReadService(repository);
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "R3",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "R3",
            TargetChuteCode = "7",
            ActualChuteCode = " 8 ",
            Status = BusinessTaskStatus.Scanned,
            CreatedTimeLocal = start.AddHours(1),
            UpdatedTimeLocal = start.AddHours(1)
        }, CancellationToken.None);

        var result = await service.QueryRecirculationsAsync(new BusinessTaskQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end,
            PageNumber = 1,
            PageSize = 10
        }, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("R3", result.Items[0].TaskCode);
    }

    /// <summary>
    /// 游标分页应返回游标语义并支持连续翻页。
    /// </summary>
    [Fact]
    public async Task QueryTasksAsync_ShouldSupportCursorPaging()
    {
        var repository = new InMemoryBusinessTaskRepository();
        var service = new BusinessTaskReadService(repository);
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);
        var createdTimes = new[]
        {
            start.AddHours(5),
            start.AddHours(4),
            start.AddHours(3)
        };

        for (var i = 0; i < createdTimes.Length; i++)
        {
            await repository.SaveAsync(new BusinessTaskEntity
            {
                TaskCode = $"C{i + 1}",
                SourceTableCode = "SRC",
                SourceType = BusinessTaskSourceType.Split,
                BusinessKey = $"C{i + 1}",
                Barcode = $"BC{i + 1}",
                TargetChuteCode = "7",
                Status = BusinessTaskStatus.Created,
                CreatedTimeLocal = createdTimes[i],
                UpdatedTimeLocal = createdTimes[i]
            }, CancellationToken.None);
        }

        var firstPage = await service.QueryTasksAsync(new BusinessTaskQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end,
            PageSize = 2,
            LastCreatedTimeLocal = end,
            LastId = long.MaxValue
        }, CancellationToken.None);

        Assert.Equal("Cursor", firstPage.PaginationMode);
        Assert.Equal(-1, firstPage.TotalCount);
        Assert.True(firstPage.HasMore);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.NotNull(firstPage.NextLastCreatedTimeLocal);
        Assert.NotNull(firstPage.NextLastId);
        Assert.Equal("C1", firstPage.Items[0].TaskCode);
        Assert.Equal("C2", firstPage.Items[1].TaskCode);

        var secondPage = await service.QueryTasksAsync(new BusinessTaskQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end,
            PageSize = 2,
            LastCreatedTimeLocal = firstPage.NextLastCreatedTimeLocal,
            LastId = firstPage.NextLastId
        }, CancellationToken.None);

        Assert.Equal("Cursor", secondPage.PaginationMode);
        Assert.Equal(-1, secondPage.TotalCount);
        Assert.False(secondPage.HasMore);
        Assert.Single(secondPage.Items);
        Assert.Null(secondPage.NextLastCreatedTimeLocal);
        Assert.Null(secondPage.NextLastId);
        Assert.Equal("C3", secondPage.Items[0].TaskCode);
    }

    /// <summary>
    /// 游标分页末页应返回无更多数据。
    /// </summary>
    [Fact]
    public async Task QueryTasksAsync_ShouldReturnNoMore_WhenCursorReachesLastPage()
    {
        var repository = new InMemoryBusinessTaskRepository();
        var service = new BusinessTaskReadService(repository);
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "C-LAST",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "C-LAST",
            Barcode = "BC-LAST",
            TargetChuteCode = "7",
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = start.AddHours(1),
            UpdatedTimeLocal = start.AddHours(1)
        }, CancellationToken.None);

        var result = await service.QueryTasksAsync(new BusinessTaskQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end,
            PageSize = 10,
            LastCreatedTimeLocal = end,
            LastId = long.MaxValue
        }, CancellationToken.None);

        Assert.Equal("Cursor", result.PaginationMode);
        Assert.Equal(-1, result.TotalCount);
        Assert.False(result.HasMore);
        Assert.Single(result.Items);
        Assert.Null(result.NextLastCreatedTimeLocal);
        Assert.Null(result.NextLastId);
    }

    /// <summary>
    /// 仅传入单侧游标参数时应回退页码分页语义。
    /// </summary>
    [Fact]
    public async Task QueryTasksAsync_ShouldFallbackToPageMode_WhenCursorParametersAreIncomplete()
    {
        var repository = new InMemoryBusinessTaskRepository();
        var service = new BusinessTaskReadService(repository);
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "P1",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "P1",
            Barcode = "BP1",
            TargetChuteCode = "7",
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = start.AddHours(1),
            UpdatedTimeLocal = start.AddHours(1)
        }, CancellationToken.None);

        var result = await service.QueryTasksAsync(new BusinessTaskQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end,
            PageNumber = 1,
            PageSize = 10,
            LastCreatedTimeLocal = end
        }, CancellationToken.None);

        Assert.Equal("PageNumber", result.PaginationMode);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
    }
}
