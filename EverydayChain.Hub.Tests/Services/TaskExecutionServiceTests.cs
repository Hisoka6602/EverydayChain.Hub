using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.ScanMatch.Services;
using EverydayChain.Hub.Application.TaskExecution.Services;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 任务执行服务单元测试。
/// </summary>
public sealed class TaskExecutionServiceTests
{
    /// <summary>
    /// 构建测试用的 TaskExecutionService。
    /// </summary>
    private static (TaskExecutionService Service, InMemoryBusinessTaskRepository Repository) CreateService()
    {
        var repo = new InMemoryBusinessTaskRepository();
        var scanLogRepo = new InMemoryScanLogRepository();
        var matchService = new ScanMatchService(repo);
        var execService = new TaskExecutionService(matchService, repo, scanLogRepo, NullLogger<TaskExecutionService>.Instance);
        return (execService, repo);
    }

    /// <summary>
    /// 无对应任务时应返回失败结果。
    /// </summary>
    [Fact]
    public async Task MarkScannedAsync_ShouldFail_WhenNoTaskFound()
    {
        var (service, _) = CreateService();
        var request = new ScanUploadApplicationRequest
        {
            Barcode = "UNKNOWN",
            DeviceCode = "DVC-01",
            ScanTimeLocal = DateTime.Now
        };

        var result = await service.MarkScannedAsync(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.FailureReason);
    }

    /// <summary>
    /// 任务处于已创建状态时应成功推进到已扫描。
    /// </summary>
    [Fact]
    public async Task MarkScannedAsync_ShouldSucceed_WhenTaskIsCreated()
    {
        var (service, repo) = CreateService();
        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-001",
            SourceTableCode = "WMS",
            BusinessKey = "K1",
            Barcode = "BC-001",
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        var request = new ScanUploadApplicationRequest
        {
            Barcode = "BC-001",
            DeviceCode = "DVC-01",
            ScanTimeLocal = DateTime.Now
        };

        var result = await service.MarkScannedAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("TASK-001", result.TaskCode);
        Assert.Equal(nameof(BusinessTaskStatus.Scanned), result.TaskStatus);
    }

    /// <summary>
    /// 任务处于已落格状态时应允许重复扫描并重新推进到已扫描。
    /// </summary>
    [Fact]
    public async Task MarkScannedAsync_ShouldSucceed_WhenTaskIsDropped()
    {
        var (service, repo) = CreateService();
        var droppedTime = new DateTime(2026, 4, 18, 9, 30, 0, DateTimeKind.Local);
        var feedbackTime = new DateTime(2026, 4, 18, 9, 45, 0, DateTimeKind.Local);
        var scanTime = new DateTime(2026, 4, 18, 10, 0, 0, DateTimeKind.Local);
        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-002",
            SourceTableCode = "WMS",
            BusinessKey = "K2",
            Barcode = "BC-002",
            TargetChuteCode = "7",
            ActualChuteCode = "7",
            Status = BusinessTaskStatus.Dropped,
            FeedbackStatus = BusinessTaskFeedbackStatus.Pending,
            IsFeedbackReported = true,
            DroppedAtLocal = droppedTime,
            FeedbackTimeLocal = feedbackTime,
            ScanCount = 2,
            CreatedTimeLocal = droppedTime,
            UpdatedTimeLocal = droppedTime
        }, CancellationToken.None);

        var request = new ScanUploadApplicationRequest
        {
            Barcode = "BC-002",
            ScanTimeLocal = scanTime
        };

        var result = await service.MarkScannedAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(BusinessTaskStatus.Scanned), result.TaskStatus);

        var updatedTask = await repo.FindByTaskCodeAsync("TASK-002", CancellationToken.None);
        Assert.NotNull(updatedTask);
        Assert.Equal(BusinessTaskStatus.Scanned, updatedTask!.Status);
        Assert.Equal(scanTime, updatedTask.ScannedAtLocal);
        Assert.Equal("7", updatedTask.TargetChuteCode);
        Assert.Null(updatedTask.ActualChuteCode);
        Assert.Null(updatedTask.DroppedAtLocal);
        Assert.Equal(BusinessTaskFeedbackStatus.NotRequired, updatedTask.FeedbackStatus);
        Assert.False(updatedTask.IsFeedbackReported);
        Assert.Null(updatedTask.FeedbackTimeLocal);
        Assert.Equal(3, updatedTask.ScanCount);
    }

    /// <summary>
    /// 扫描成功后仓储中任务状态应更新为已扫描。
    /// </summary>
    [Fact]
    public async Task MarkScannedAsync_ShouldPersistScannedStatus()
    {
        var (service, repo) = CreateService();
        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-003",
            SourceTableCode = "WMS",
            BusinessKey = "K3",
            Barcode = "BC-003",
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        var request = new ScanUploadApplicationRequest
        {
            Barcode = "BC-003",
            DeviceCode = "DVC-02",
            ScanTimeLocal = new DateTime(2026, 4, 13, 10, 0, 0, DateTimeKind.Local),
            LengthMm = 120,
            WidthMm = 220,
            HeightMm = 320,
            VolumeMm3 = 8448000,
            WeightGram = 520
        };

        await service.MarkScannedAsync(request, CancellationToken.None);

        var updatedTask = await repo.FindByTaskCodeAsync("TASK-003", CancellationToken.None);
        Assert.NotNull(updatedTask);
        Assert.Equal(BusinessTaskStatus.Scanned, updatedTask!.Status);
        Assert.Equal("DVC-02", updatedTask.DeviceCode);
        Assert.Equal(new DateTime(2026, 4, 13, 10, 0, 0, DateTimeKind.Local), updatedTask.ScannedAtLocal);
        Assert.Equal(120, updatedTask.LengthMm);
        Assert.Equal(220, updatedTask.WidthMm);
        Assert.Equal(320, updatedTask.HeightMm);
        Assert.Equal(8448000, updatedTask.VolumeMm3);
        Assert.Equal(520, updatedTask.WeightGram);
        Assert.Equal(1, updatedTask.ScanCount);
    }

    /// <summary>
    /// 请求包含目标格口编码时应覆盖旧值并刷新归并码头编码。
    /// </summary>
    [Fact]
    public async Task MarkScannedAsync_ShouldOverwriteTargetChuteCode_WhenRequestHasTargetChuteCode()
    {
        var (service, repo) = CreateService();
        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-004",
            SourceTableCode = "WMS",
            BusinessKey = "K4",
            Barcode = "BC-004",
            TargetChuteCode = "9",
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        var request = new ScanUploadApplicationRequest
        {
            Barcode = "BC-004",
            ScanTimeLocal = DateTime.Now,
            TargetChuteCode = "1"
        };

        var result = await service.MarkScannedAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updatedTask = await repo.FindByTaskCodeAsync("TASK-004", CancellationToken.None);
        Assert.NotNull(updatedTask);
        Assert.Equal("1", updatedTask!.TargetChuteCode);
        Assert.Equal("1", updatedTask.ResolvedDockCode);
    }

    /// <summary>
    /// 请求未提供目标格口编码时应保留旧值。
    /// </summary>
    [Fact]
    public async Task MarkScannedAsync_ShouldKeepTargetChuteCode_WhenRequestHasNoTargetChuteCode()
    {
        var (service, repo) = CreateService();
        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-005",
            SourceTableCode = "WMS",
            BusinessKey = "K5",
            Barcode = "BC-005",
            TargetChuteCode = "7",
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        var request = new ScanUploadApplicationRequest
        {
            Barcode = "BC-005",
            ScanTimeLocal = DateTime.Now,
            TargetChuteCode = null
        };

        var result = await service.MarkScannedAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updatedTask = await repo.FindByTaskCodeAsync("TASK-005", CancellationToken.None);
        Assert.NotNull(updatedTask);
        Assert.Equal("7", updatedTask!.TargetChuteCode);
        Assert.Equal("7", updatedTask.ResolvedDockCode);
    }

    /// <summary>
    /// 扫描失败时不应错误覆盖任务目标格口编码。
    /// </summary>
    [Fact]
    public async Task MarkScannedAsync_ShouldNotUpdateTargetChuteCode_WhenTaskStateIsInvalid()
    {
        var (service, repo) = CreateService();
        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-006",
            SourceTableCode = "WMS",
            BusinessKey = "K6",
            Barcode = "BC-006",
            TargetChuteCode = "8",
            Status = BusinessTaskStatus.Exception,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        var request = new ScanUploadApplicationRequest
        {
            Barcode = "BC-006",
            ScanTimeLocal = DateTime.Now,
            TargetChuteCode = "2"
        };

        var result = await service.MarkScannedAsync(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        var updatedTask = await repo.FindByTaskCodeAsync("TASK-006", CancellationToken.None);
        Assert.NotNull(updatedTask);
        Assert.Equal("8", updatedTask!.TargetChuteCode);
    }
}
