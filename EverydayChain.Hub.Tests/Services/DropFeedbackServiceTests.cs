using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Services;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 落格回传服务测试（接入真实仓储替身）。
/// </summary>
public sealed class DropFeedbackServiceTests
{
    /// <summary>
    /// 构建测试用的 DropFeedbackService。
    /// </summary>
    private static (DropFeedbackService Service, InMemoryBusinessTaskRepository Repository) CreateService()
    {
        var repo = new InMemoryBusinessTaskRepository();
        var dropLogRepo = new InMemoryDropLogRepository();
        var service = new DropFeedbackService(repo, dropLogRepo, NullLogger<DropFeedbackService>.Instance);
        return (service, repo);
    }

    /// <summary>
    /// TaskCode 和 Barcode 同时为空时应返回参数校验失败。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenBothTaskCodeAndBarcodeAreEmpty()
    {
        var (service, _) = CreateService();
        var request = new DropFeedbackApplicationRequest
        {
            TaskCode = string.Empty,
            Barcode = string.Empty,
            ActualChuteCode = "CHUTE-01",
            DropTimeLocal = DateTime.Now,
            IsSuccess = true
        };

        var result = await service.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.IsAccepted);
        Assert.NotEmpty(result.FailureReason);
    }

    /// <summary>
    /// 任务不存在时应返回失败结果。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenTaskNotFound()
    {
        var (service, _) = CreateService();
        var request = new DropFeedbackApplicationRequest
        {
            Barcode = "UNKNOWN",
            ActualChuteCode = "CHUTE-01",
            DropTimeLocal = DateTime.Now,
            IsSuccess = true
        };

        var result = await service.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.IsAccepted);
        Assert.Equal("TaskNotFound", result.FailureReason);
    }

    /// <summary>
    /// TaskCode 与 Barcode 同时提供但不匹配时应返回参数冲突失败。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenBarcodeConflictsWithTaskCode()
    {
        var (service, repo) = CreateService();
        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-001",
            SourceTableCode = "WMS",
            BusinessKey = "K1",
            Barcode = "BC-001",
            Status = BusinessTaskStatus.Scanned,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        var request = new DropFeedbackApplicationRequest
        {
            TaskCode = "TASK-001",
            Barcode = "WRONG-BC",
            ActualChuteCode = "CHUTE-01",
            DropTimeLocal = DateTime.Now,
            IsSuccess = true
        };

        var result = await service.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.IsAccepted);
        Assert.Equal("BarcodeMismatch", result.FailureReason);
    }

    /// <summary>
    /// 任务状态不是已扫描时应拒绝落格回传。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenTaskIsNotScanned()
    {
        var (service, repo) = CreateService();
        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-002",
            SourceTableCode = "WMS",
            BusinessKey = "K2",
            Barcode = "BC-002",
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        var request = new DropFeedbackApplicationRequest
        {
            Barcode = "BC-002",
            ActualChuteCode = "CHUTE-01",
            DropTimeLocal = DateTime.Now,
            IsSuccess = true
        };

        var result = await service.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.IsAccepted);
        Assert.Equal("InvalidTaskStatus", result.FailureReason);
    }

    /// <summary>
    /// 落格成功时任务应推进到 Dropped 状态。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldTransitionToDropped_WhenSuccessIsTrue()
    {
        var (service, repo) = CreateService();
        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-003",
            SourceTableCode = "WMS",
            BusinessKey = "K3",
            Barcode = "BC-003",
            Status = BusinessTaskStatus.Scanned,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        var dropTime = new DateTime(2026, 4, 13, 14, 30, 0, DateTimeKind.Local);
        var request = new DropFeedbackApplicationRequest
        {
            Barcode = "BC-003",
            ActualChuteCode = "CHUTE-A1",
            DropTimeLocal = dropTime,
            IsSuccess = true
        };

        var result = await service.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.IsAccepted);
        Assert.Equal("TASK-003", result.TaskCode);
        Assert.Equal(nameof(BusinessTaskStatus.Dropped), result.Status);

        var updated = await repo.FindByTaskCodeAsync("TASK-003", CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal(BusinessTaskStatus.Dropped, updated!.Status);
        Assert.Equal("CHUTE-A1", updated.ActualChuteCode);
        Assert.Equal(dropTime, updated.DroppedAtLocal);
    }

    /// <summary>
    /// 落格失败时任务应推进到 Exception 状态。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldTransitionToException_WhenSuccessIsFalse()
    {
        var (service, repo) = CreateService();
        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-004",
            SourceTableCode = "WMS",
            BusinessKey = "K4",
            Barcode = "BC-004",
            Status = BusinessTaskStatus.Scanned,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        var request = new DropFeedbackApplicationRequest
        {
            Barcode = "BC-004",
            ActualChuteCode = "CHUTE-B1",
            DropTimeLocal = DateTime.Now,
            IsSuccess = false,
            FailureReason = "分拣臂故障"
        };

        var result = await service.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.IsAccepted);
        Assert.Equal("TASK-004", result.TaskCode);
        Assert.Equal(nameof(BusinessTaskStatus.Exception), result.Status);

        var updated = await repo.FindByTaskCodeAsync("TASK-004", CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal(BusinessTaskStatus.Exception, updated!.Status);
        Assert.Equal("分拣臂故障", updated.FailureReason);
    }
}
