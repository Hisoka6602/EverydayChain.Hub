using EverydayChain.Hub.Application.Abstractions.Integrations;
using EverydayChain.Hub.Application.Feedback.Services;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Integrations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 WmsFeedbackServiceTests 类型。
/// </summary>
public sealed class WmsFeedbackServiceTests
{
    private static (WmsFeedbackService Service, InMemoryBusinessTaskRepository Repository, CapturingWmsOracleFeedbackGateway Writer) CreateService(bool enabled = true)
    {
        var repo = new InMemoryBusinessTaskRepository();
        var writer = new CapturingWmsOracleFeedbackGateway();
        var options = new WmsFeedbackOptions { Enabled = enabled };
        var service = new WmsFeedbackService(repo, writer, options, NullLogger<WmsFeedbackService>.Instance);
        return (service, repo, writer);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmpty_WhenNoPendingTasks()
    {
        var (service, _, writer) = CreateService();

        var result = await service.ExecuteAsync(100, CancellationToken.None);

        Assert.Equal(0, result.PendingCount);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.True(result.IsSuccess);
        Assert.Empty(writer.CapturedTasks);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMarkCompleted_WhenWriterSucceeds()
    {
        var (service, repo, writer) = CreateService();

        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-001",
            SourceTableCode = "WMS",
            BusinessKey = "K1",
            Status = BusinessTaskStatus.Dropped,
            FeedbackStatus = BusinessTaskFeedbackStatus.Pending,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        writer.ReturnCount = 1;

        var result = await service.ExecuteAsync(100, CancellationToken.None);

        Assert.Equal(1, result.PendingCount);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.True(result.IsSuccess);

        var updated = await repo.FindByTaskCodeAsync("TASK-001", CancellationToken.None);
        Assert.Equal(BusinessTaskFeedbackStatus.Completed, updated!.FeedbackStatus);
        Assert.True(updated.IsFeedbackReported);
        Assert.NotNull(updated.FeedbackTimeLocal);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMarkFailed_WhenWriterThrows()
    {
        var (service, repo, writer) = CreateService();

        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-002",
            SourceTableCode = "WMS",
            BusinessKey = "K2",
            Status = BusinessTaskStatus.Dropped,
            FeedbackStatus = BusinessTaskFeedbackStatus.Pending,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        writer.ThrowOnWrite = true;

        var result = await service.ExecuteAsync(100, CancellationToken.None);

        Assert.Equal(1, result.PendingCount);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.False(result.IsSuccess);

        var updated = await repo.FindByTaskCodeAsync("TASK-002", CancellationToken.None);
        Assert.Equal(BusinessTaskFeedbackStatus.Failed, updated!.FeedbackStatus);
        Assert.False(updated.IsFeedbackReported);
        Assert.Null(updated.FeedbackTimeLocal);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRespectBatchSize()
    {
        var (service, repo, writer) = CreateService();
        writer.ReturnCount = 2;

        for (var i = 1; i <= 5; i++)
        {
            await repo.SaveAsync(new BusinessTaskEntity
            {
                TaskCode = $"TASK-{i:D3}",
                SourceTableCode = "WMS",
                BusinessKey = $"K{i}",
                Status = BusinessTaskStatus.Dropped,
                FeedbackStatus = BusinessTaskFeedbackStatus.Pending,
                CreatedTimeLocal = DateTime.Now,
                UpdatedTimeLocal = DateTime.Now
            }, CancellationToken.None);
        }

        var result = await service.ExecuteAsync(2, CancellationToken.None);

        Assert.Equal(2, result.PendingCount);
        Assert.Equal(2, writer.CapturedTasks.Count);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmpty_WhenDisabled()
    {
        var (service, repo, writer) = CreateService(enabled: false);

        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-D01",
            SourceTableCode = "WMS",
            BusinessKey = "KD1",
            Status = BusinessTaskStatus.Dropped,
            FeedbackStatus = BusinessTaskFeedbackStatus.Pending,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        var result = await service.ExecuteAsync(100, CancellationToken.None);

        Assert.Equal(0, result.PendingCount);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.True(result.IsSuccess);
        Assert.Empty(writer.CapturedTasks);

        var task = await repo.FindByTaskCodeAsync("TASK-D01", CancellationToken.None);
        Assert.Equal(BusinessTaskFeedbackStatus.Pending, task!.FeedbackStatus);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMarkFailed_WhenWrittenRowsMismatch()
    {
        var (service, repo, writer) = CreateService();

        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-M01",
            SourceTableCode = "WMS",
            BusinessKey = "KM1",
            Status = BusinessTaskStatus.Dropped,
            FeedbackStatus = BusinessTaskFeedbackStatus.Pending,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        writer.ReturnCount = 0;

        var result = await service.ExecuteAsync(100, CancellationToken.None);

        Assert.Equal(1, result.PendingCount);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.False(result.IsSuccess);

        var updated = await repo.FindByTaskCodeAsync("TASK-M01", CancellationToken.None);
        Assert.Equal(BusinessTaskFeedbackStatus.Failed, updated!.FeedbackStatus);
        Assert.False(updated.IsFeedbackReported);
        Assert.Null(updated.FeedbackTimeLocal);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCompleteSplitTask_WhenSplitTaskWritten()
    {
        var (service, repo, writer) = CreateService();
        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-SPLIT-001",
            SourceTableCode = "WMS_SPLIT",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "KS1",
            Status = BusinessTaskStatus.Dropped,
            FeedbackStatus = BusinessTaskFeedbackStatus.Pending,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);
        writer.ReturnCount = 1;

        var result = await service.ExecuteAsync(100, CancellationToken.None);

        Assert.Equal(1, result.SuccessCount);
        var updated = await repo.FindByTaskCodeAsync("TASK-SPLIT-001", CancellationToken.None);
        Assert.Equal(BusinessTaskFeedbackStatus.Completed, updated!.FeedbackStatus);
        Assert.True(updated.IsFeedbackReported);
        Assert.Contains(writer.CapturedTasks, task => task.SourceType == BusinessTaskSourceType.Split);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCompleteFullCaseTask_WhenFullCaseTaskWritten()
    {
        var (service, repo, writer) = CreateService();
        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-FULL-001",
            SourceTableCode = "WMS_FULLCASE",
            SourceType = BusinessTaskSourceType.FullCase,
            BusinessKey = "KF1",
            Status = BusinessTaskStatus.Dropped,
            FeedbackStatus = BusinessTaskFeedbackStatus.Pending,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);
        writer.ReturnCount = 1;

        var result = await service.ExecuteAsync(100, CancellationToken.None);

        Assert.Equal(1, result.SuccessCount);
        var updated = await repo.FindByTaskCodeAsync("TASK-FULL-001", CancellationToken.None);
        Assert.Equal(BusinessTaskFeedbackStatus.Completed, updated!.FeedbackStatus);
        Assert.True(updated.IsFeedbackReported);
        Assert.Contains(writer.CapturedTasks, task => task.SourceType == BusinessTaskSourceType.FullCase);
    }

    [Fact]
    public void ResolveTargetBySourceType_ShouldRouteToConfiguredTables()
    {
        var gateway = CreateGatewayForSourceTypeRoutingTests();
        var resolveMethod = GetResolveTargetBySourceTypeMethod();

        var splitTarget = ((string Schema, string Table, string BusinessKeyColumn))resolveMethod.Invoke(gateway, [BusinessTaskSourceType.Split])!;
        var fullCaseTarget = ((string Schema, string Table, string BusinessKeyColumn))resolveMethod.Invoke(gateway, [BusinessTaskSourceType.FullCase])!;

        Assert.Equal("WMS_USER_SPLIT_431", splitTarget.Schema);
        Assert.Equal("IDX_PICKTOLIGHT_CARTON1", splitTarget.Table);
        Assert.Equal("SPLIT_TASK_CODE", splitTarget.BusinessKeyColumn);
        Assert.Equal("WMS_USER_FULLCASE_431", fullCaseTarget.Schema);
        Assert.Equal("IDX_PICKTOWCS2", fullCaseTarget.Table);
        Assert.Equal("FULLCASE_TASK_CODE", fullCaseTarget.BusinessKeyColumn);
    }

    [Fact]
    public void ResolveTargetBySourceType_ShouldThrowChineseException_WhenSourceTypeUnsupported()
    {
        var gateway = CreateGatewayForSourceTypeRoutingTests();
        var resolveMethod = GetResolveTargetBySourceTypeMethod();

        var exception = Assert.Throws<TargetInvocationException>(() =>
            resolveMethod.Invoke(gateway, [BusinessTaskSourceType.Unknown]));
        var innerException = Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Contains("不支持的业务来源类型，无法确定 WMS 回写目标表。", innerException.Message, StringComparison.Ordinal);
    }

    private static OracleWmsFeedbackGateway CreateGatewayForSourceTypeRoutingTests()
    {
        var options = Options.Create(new WmsFeedbackOptions
        {
            Enabled = true,
            SplitSchema = "WMS_USER_SPLIT_431",
            SplitTable = "IDX_PICKTOLIGHT_CARTON1",
            SplitBusinessKeyColumn = "SPLIT_TASK_CODE",
            FullCaseSchema = "WMS_USER_FULLCASE_431",
            FullCaseTable = "IDX_PICKTOWCS2",
            FullCaseBusinessKeyColumn = "FULLCASE_TASK_CODE",
            FeedbackStatusColumn = "STATUS",
            FeedbackCompletedValue = "Y"
        });
        var oracleOptions = Options.Create(new OracleOptions
        {
            ConnectionString = "Data Source=127.0.0.1:1521/ORCL;User Id=PLACEHOLDER_USER;Password=PLACEHOLDER_PASSWORD;"
        });
        return new OracleWmsFeedbackGateway(
            options,
            oracleOptions,
            new PassThroughDangerZoneExecutor(),
            NullLogger<OracleWmsFeedbackGateway>.Instance);
    }

    private static MethodInfo GetResolveTargetBySourceTypeMethod()
    {
        return typeof(OracleWmsFeedbackGateway).GetMethod(
                "ResolveTargetBySourceType",
                BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("未找到 ResolveTargetBySourceType 方法。");
    }
}

/// <summary>
/// 定义 CapturingWmsOracleFeedbackGateway 类型。
/// </summary>
internal sealed class CapturingWmsOracleFeedbackGateway : IWmsOracleFeedbackGateway
{
    /// <summary>
    /// 获取或设置 CapturedTasks。
    /// </summary>
    public List<BusinessTaskEntity> CapturedTasks { get; } = [];

    /// <summary>
    /// 获取或设置 ThrowOnWrite。
    /// </summary>
    public bool ThrowOnWrite { get; set; }

    /// <summary>
    /// 获取或设置 ReturnCount。
    /// </summary>
    public int? ReturnCount { get; set; }

    public Task<int> WriteFeedbackAsync(IReadOnlyList<BusinessTaskEntity> tasks, CancellationToken ct)
    {
        CapturedTasks.AddRange(tasks);

        if (ThrowOnWrite)
        {
            throw new InvalidOperationException("模拟 WMS Oracle 网关异常。");
        }

        return Task.FromResult(ReturnCount ?? tasks.Count);
    }
}

