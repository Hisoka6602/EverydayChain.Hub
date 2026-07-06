using EverydayChain.Hub.Application.Abstractions.Integrations;
using EverydayChain.Hub.Application.Feedback.Services;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class FeedbackCompensationServiceTests
{
    private static (FeedbackCompensationService Service, InMemoryBusinessTaskRepository Repository, CapturingCompensationGateway Gateway) CreateService()
    {
        var repository = new InMemoryBusinessTaskRepository();
        var gateway = new CapturingCompensationGateway();
        var service = new FeedbackCompensationService(repository, gateway, NullLogger<FeedbackCompensationService>.Instance);
        return (service, repository, gateway);
    }

    [Fact]
    public async Task RetryFailedBatchAsync_ShouldMarkCompleted_WhenGatewaySucceeds()
    {
        var (service, repository, gateway) = CreateService();
        await repository.SaveAsync(CreateFailedTask("TASK-COMP-001"), CancellationToken.None);
        gateway.ReturnCount = 1;

        var result = await service.RetryFailedBatchAsync(10, CancellationToken.None);

        Assert.Equal(1, result.TargetCount);
        Assert.Equal(1, result.RetriedCount);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        var updated = await repository.FindByTaskCodeAsync("TASK-COMP-001", CancellationToken.None);
        Assert.Equal(BusinessTaskFeedbackStatus.Completed, updated!.FeedbackStatus);
        Assert.True(updated.IsFeedbackReported);
        Assert.NotNull(updated.FeedbackTimeLocal);
    }

    [Fact]
    public async Task RetryFailedBatchAsync_ShouldKeepFailed_WhenGatewayThrows()
    {
        var (service, repository, gateway) = CreateService();
        await repository.SaveAsync(CreateFailedTask("TASK-COMP-002"), CancellationToken.None);
        gateway.ThrowOnWrite = true;

        var result = await service.RetryFailedBatchAsync(10, CancellationToken.None);

        Assert.Equal(1, result.TargetCount);
        Assert.Equal(1, result.FailedCount);
        Assert.False(result.IsSuccess);
        var updated = await repository.FindByTaskCodeAsync("TASK-COMP-002", CancellationToken.None);
        Assert.Equal(BusinessTaskFeedbackStatus.Failed, updated!.FeedbackStatus);
        Assert.False(updated.IsFeedbackReported);
        Assert.Null(updated.FeedbackTimeLocal);
    }

    [Fact]
    public async Task RetryByTaskCodeAsync_ShouldSkip_WhenTaskNotFailed()
    {
        var (service, repository, gateway) = CreateService();
        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-COMP-003",
            SourceTableCode = "WMS",
            BusinessKey = "KEY-3",
            Status = BusinessTaskStatus.Dropped,
            FeedbackStatus = BusinessTaskFeedbackStatus.Completed,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        var result = await service.RetryByTaskCodeAsync("TASK-COMP-003", CancellationToken.None);

        Assert.Equal(1, result.TargetCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Empty(gateway.CapturedTasks);
    }

    [Fact]
    public async Task RetryByTaskCodeAsync_ShouldRetryOneTask_WhenFailedTaskExists()
    {
        var (service, repository, gateway) = CreateService();
        await repository.SaveAsync(CreateFailedTask("TASK-COMP-004"), CancellationToken.None);
        await repository.SaveAsync(CreateFailedTask("TASK-COMP-005"), CancellationToken.None);
        gateway.ReturnCount = 1;

        var result = await service.RetryByTaskCodeAsync("TASK-COMP-004", CancellationToken.None);

        Assert.Equal(1, result.TargetCount);
        Assert.Equal(1, result.SuccessCount);
        Assert.Single(gateway.CapturedTasks);
        Assert.Equal("TASK-COMP-004", gateway.CapturedTasks[0].TaskCode);
    }

    private static BusinessTaskEntity CreateFailedTask(string taskCode)
    {
        return new BusinessTaskEntity
        {
            TaskCode = taskCode,
            SourceTableCode = "WMS",
            BusinessKey = $"{taskCode}-KEY",
            Status = BusinessTaskStatus.Dropped,
            FeedbackStatus = BusinessTaskFeedbackStatus.Failed,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        };
    }

    /// <summary>
    /// 定义当前类型。
    /// </summary>
    private sealed class CapturingCompensationGateway : IWmsOracleFeedbackGateway
    {
        /// <summary>
        /// 获取或设置当前属性值。
        /// </summary>
        public List<BusinessTaskEntity> CapturedTasks { get; } = [];

        /// <summary>
        /// 获取或设置当前属性值。
        /// </summary>
        public bool ThrowOnWrite { get; set; }

        /// <summary>
        /// 获取或设置当前属性值。
        /// </summary>
        public int? ReturnCount { get; set; }

        public Task<int> WriteFeedbackAsync(IReadOnlyList<BusinessTaskEntity> tasks, CancellationToken ct)
        {
            CapturedTasks.AddRange(tasks);
            if (ThrowOnWrite)
            {
                throw new InvalidOperationException("模拟补偿写入失败。");
            }

            return Task.FromResult(ReturnCount ?? tasks.Count);
        }
    }
}

