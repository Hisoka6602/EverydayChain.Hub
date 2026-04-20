using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Queries;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 总看板查询服务测试。
/// </summary>
public sealed class GlobalDashboardQueryServiceTests
{
    /// <summary>
    /// 构建测试服务。
    /// </summary>
    /// <returns>服务与仓储替身。</returns>
    private static (GlobalDashboardQueryService Service, InMemoryBusinessTaskRepository Repository) CreateService()
    {
        var repository = new InMemoryBusinessTaskRepository();
        var service = new GlobalDashboardQueryService(repository);
        return (service, repository);
    }

    /// <summary>
    /// 空数据时应返回全零统计。
    /// </summary>
    [Fact]
    public async Task QueryAsync_ShouldReturnZeroResult_WhenNoTaskExists()
    {
        var (service, _) = CreateService();
        var result = await service.QueryAsync(new GlobalDashboardQueryRequest
        {
            StartTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 1, 0, 0, 0), DateTimeKind.Local),
            EndTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 2, 0, 0, 0), DateTimeKind.Local)
        }, CancellationToken.None);

        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0M, result.TotalSortedProgressPercent);
        Assert.Equal(0M, result.RecognitionRatePercent);
        Assert.Empty(result.WaveSummaries);
    }

    /// <summary>
    /// 存在任务时应正确聚合总看板指标。
    /// </summary>
    [Fact]
    public async Task QueryAsync_ShouldAggregateMetrics_WhenTasksExist()
    {
        var (service, repository) = CreateService();
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 1, 0, 0, 0), DateTimeKind.Local);
        var end = DateTime.SpecifyKind(new DateTime(2026, 4, 2, 0, 0, 0), DateTimeKind.Local);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-001",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.FullCase,
            BusinessKey = "KEY-001",
            WaveCode = "WAVE-001",
            Status = BusinessTaskStatus.Dropped,
            ScannedAtLocal = start.AddHours(1),
            ActualChuteCode = "6",
            IsException = false,
            VolumeMm3 = 100M,
            WeightGram = 10M,
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
            Status = BusinessTaskStatus.Created,
            ActualChuteCode = "8",
            IsException = true,
            VolumeMm3 = 200M,
            WeightGram = 20M,
            CreatedTimeLocal = start.AddMinutes(2),
            UpdatedTimeLocal = start.AddMinutes(2)
        }, CancellationToken.None);

        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-003",
            SourceTableCode = "SRC",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "KEY-003",
            WaveCode = null,
            Status = BusinessTaskStatus.FeedbackPending,
            ScannedAtLocal = start.AddHours(2),
            ActualChuteCode = "7",
            IsException = false,
            VolumeMm3 = 300M,
            WeightGram = 30M,
            CreatedTimeLocal = start.AddMinutes(3),
            UpdatedTimeLocal = start.AddMinutes(3)
        }, CancellationToken.None);

        var result = await service.QueryAsync(new GlobalDashboardQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end
        }, CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, result.UnsortedCount);
        Assert.Equal(66.67M, result.TotalSortedProgressPercent);
        Assert.Equal(1, result.FullCaseTotalCount);
        Assert.Equal(0, result.FullCaseUnsortedCount);
        Assert.Equal(100M, result.FullCaseSortedProgressPercent);
        Assert.Equal(2, result.SplitTotalCount);
        Assert.Equal(1, result.SplitUnsortedCount);
        Assert.Equal(50M, result.SplitSortedProgressPercent);
        Assert.Equal(66.67M, result.RecognitionRatePercent);
        Assert.Equal(1, result.RecirculatedCount);
        Assert.Equal(1, result.ExceptionCount);
        Assert.Equal(600M, result.TotalVolumeMm3);
        Assert.Equal(60M, result.TotalWeightGram);
        Assert.Equal(2, result.WaveSummaries.Count);
        Assert.Contains(result.WaveSummaries, x => x.WaveCode == "WAVE-001" && x.TotalCount == 2 && x.UnsortedCount == 1);
        Assert.Contains(result.WaveSummaries, x => x.WaveCode == "未分波次" && x.TotalCount == 1 && x.UnsortedCount == 0);
    }
}
