using EverydayChain.Hub.Application.Abstractions.Integrations;
using EverydayChain.Hub.Application.Feedback.Services;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 业务回传服务单元测试。
/// </summary>
public sealed class WmsFeedbackServiceTests
{
    /// <summary>
    /// 构建测试用的 WmsFeedbackService，注入内存替身。
    /// </summary>
    private static (WmsFeedbackService Service, InMemoryBusinessTaskRepository Repository, CapturingWmsOracleFeedbackGateway Writer) CreateService()
    {
        var repo = new InMemoryBusinessTaskRepository();
        var writer = new CapturingWmsOracleFeedbackGateway();
        var service = new WmsFeedbackService(repo, writer, NullLogger<WmsFeedbackService>.Instance);
        return (service, repo, writer);
    }

    /// <summary>
    /// 无待回传任务时应返回空结果且不调用写入器。
    /// </summary>
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

    /// <summary>
    /// 有待回传任务且写入成功时，任务应标记为已回传。
    /// </summary>
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
    }

    /// <summary>
    /// 写入器抛出异常时，所有任务应标记为回传失败。
    /// </summary>
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
    }

    /// <summary>
    /// batchSize 参数应限制查询上限。
    /// </summary>
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
}

/// <summary>
/// 捕获写入请求的 WMS Oracle 网关测试替身。
/// </summary>
internal sealed class CapturingWmsOracleFeedbackGateway : IWmsOracleFeedbackGateway
{
    /// <summary>捕获到的任务列表。</summary>
    public List<BusinessTaskEntity> CapturedTasks { get; } = [];

    /// <summary>模拟网关抛出异常。</summary>
    public bool ThrowOnWrite { get; set; }

    /// <summary>网关返回的行数；默认与输入列表长度相同。</summary>
    public int? ReturnCount { get; set; }

    /// <inheritdoc/>
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
