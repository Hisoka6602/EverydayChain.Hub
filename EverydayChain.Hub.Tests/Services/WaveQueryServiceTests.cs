using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Queries;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 波次查询服务测试。
/// </summary>
public sealed class WaveQueryServiceTests
{
    /// <summary>
    /// 波次选项查询应按波次去重并返回备注。
    /// </summary>
    [Fact]
    public async Task QueryOptionsAsync_ShouldReturnDistinctWaveOptionsWithRemark()
    {
        var repository = new InMemoryBusinessTaskRepository();
        var service = new WaveQueryService(repository, NullLogger<WaveQueryService>.Instance);
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);

        await repository.SaveAsync(CreateTask("T1", "W1", "备注A", "1", BusinessTaskSourceType.Split, "2", start.AddHours(1)), CancellationToken.None);
        await repository.SaveAsync(CreateTask("T2", "W1", "备注A-更新", "1", BusinessTaskSourceType.Split, "2", start.AddHours(2)), CancellationToken.None);
        await repository.SaveAsync(CreateTask("T3", "W2", "备注B", "2", BusinessTaskSourceType.Split, "9", start.AddHours(3)), CancellationToken.None);

        var result = await service.QueryOptionsAsync(new WaveOptionsQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end
        }, CancellationToken.None);

        Assert.Equal(2, result.WaveOptions.Count);
        Assert.Equal("W1", result.WaveOptions[0].WaveCode);
        Assert.Equal("备注A-更新", result.WaveOptions[0].WaveRemark);
        Assert.Equal("W2", result.WaveOptions[1].WaveCode);
    }

    /// <summary>
    /// 波次摘要查询应按归并码头口径统计回流数。
    /// </summary>
    [Fact]
    public async Task QuerySummaryAsync_ShouldUseResolvedDockCodeRuleForRecirculation()
    {
        var repository = new InMemoryBusinessTaskRepository();
        var service = new WaveQueryService(repository, NullLogger<WaveQueryService>.Instance);
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);

        await repository.SaveAsync(CreateTask("S1", "W100", "摘要备注", "1", BusinessTaskSourceType.Split, "8", start.AddHours(1), BusinessTaskStatus.Dropped), CancellationToken.None);
        await repository.SaveAsync(CreateTask("S2", "W100", "摘要备注", "2", BusinessTaskSourceType.Split, "7", start.AddHours(2), BusinessTaskStatus.Scanned), CancellationToken.None);
        await repository.SaveAsync(CreateTask("S3", "W100", "摘要备注", "3", BusinessTaskSourceType.Split, "未分配码头", start.AddHours(3), BusinessTaskStatus.Exception, isException: true), CancellationToken.None);

        var result = await service.QuerySummaryAsync(new WaveSummaryQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end,
            WaveCode = "W100"
        }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, result!.TotalCount);
        Assert.Equal(2, result.UnsortedCount);
        Assert.Equal(33.33M, result.SortedProgressPercent);
        Assert.Equal(1, result.RecirculatedCount);
        Assert.Equal(1, result.ExceptionCount);
    }

    /// <summary>
    /// 波次分区查询应固定返回五个分组并跳过非法工作区域。
    /// </summary>
    [Fact]
    public async Task QueryZonesAsync_ShouldReturnFiveFixedZonesAndSkipInvalidWorkingArea()
    {
        var repository = new InMemoryBusinessTaskRepository();
        var service = new WaveQueryService(repository, NullLogger<WaveQueryService>.Instance);
        var start = DateTime.SpecifyKind(new DateTime(2026, 4, 20, 0, 0, 0), DateTimeKind.Local);
        var end = start.AddDays(1);

        await repository.SaveAsync(CreateTask("Z1", "W200", "分区备注", "1", BusinessTaskSourceType.Split, "8", start.AddHours(1), BusinessTaskStatus.Dropped), CancellationToken.None);
        await repository.SaveAsync(CreateTask("Z2", "W200", "分区备注", "99", BusinessTaskSourceType.Split, "8", start.AddHours(2), BusinessTaskStatus.Scanned), CancellationToken.None);
        await repository.SaveAsync(CreateTask("Z3", "W200", "分区备注", null, BusinessTaskSourceType.FullCase, "6", start.AddHours(3), BusinessTaskStatus.Scanned), CancellationToken.None);

        var result = await service.QueryZonesAsync(new WaveZoneQueryRequest
        {
            StartTimeLocal = start,
            EndTimeLocal = end,
            WaveCode = "W200"
        }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(5, result!.Zones.Count);
        Assert.Equal("SplitZone1", result.Zones[0].ZoneCode);
        Assert.Equal(1, result.Zones[0].TotalCount);
        Assert.Equal(1, result.Zones[0].RecirculatedCount);
        Assert.Equal("SplitZone2", result.Zones[1].ZoneCode);
        Assert.Equal(0, result.Zones[1].TotalCount);
        Assert.Equal("SplitZone3", result.Zones[2].ZoneCode);
        Assert.Equal(0, result.Zones[2].TotalCount);
        Assert.Equal("SplitZone4", result.Zones[3].ZoneCode);
        Assert.Equal(0, result.Zones[3].TotalCount);
        Assert.Equal("FullCase", result.Zones[4].ZoneCode);
        Assert.Equal(1, result.Zones[4].TotalCount);
        Assert.Equal(0, result.Zones[4].RecirculatedCount);
    }

    /// <summary>
    /// 创建测试任务。
    /// </summary>
    /// <param name="taskCode">任务编码。</param>
    /// <param name="waveCode">波次号。</param>
    /// <param name="waveRemark">波次备注。</param>
    /// <param name="workingArea">工作区域。</param>
    /// <param name="sourceType">来源类型。</param>
    /// <param name="resolvedDockCode">归并码头编码。</param>
    /// <param name="createdTimeLocal">创建时间。</param>
    /// <param name="status">任务状态。</param>
    /// <param name="isException">是否异常。</param>
    /// <returns>业务任务实体。</returns>
    private static BusinessTaskEntity CreateTask(
        string taskCode,
        string waveCode,
        string waveRemark,
        string? workingArea,
        BusinessTaskSourceType sourceType,
        string resolvedDockCode,
        DateTime createdTimeLocal,
        BusinessTaskStatus status = BusinessTaskStatus.Scanned,
        bool isException = false)
    {
        return new BusinessTaskEntity
        {
            TaskCode = taskCode,
            SourceTableCode = "SRC",
            SourceType = sourceType,
            BusinessKey = taskCode,
            WaveCode = waveCode,
            WaveRemark = waveRemark,
            WorkingArea = workingArea,
            Status = status,
            IsException = isException,
            ActualChuteCode = resolvedDockCode,
            CreatedTimeLocal = createdTimeLocal,
            UpdatedTimeLocal = createdTimeLocal
        };
    }
}
