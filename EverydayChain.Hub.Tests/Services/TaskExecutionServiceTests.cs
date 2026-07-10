using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.ScanMatch.Services;
using EverydayChain.Hub.Application.TaskExecution.Services;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 TaskExecutionServiceTests 类型。
/// </summary>
public sealed class TaskExecutionServiceTests
{
    private static (TaskExecutionService Service, InMemoryBusinessTaskRepository Repository, InMemoryScanLogRepository ScanLogRepository) CreateService()
    {
        var repo = new InMemoryBusinessTaskRepository();
        var scanLogRepo = new InMemoryScanLogRepository();
        var matchService = new ScanMatchService(repo);
        var execService = new TaskExecutionService(matchService, repo, scanLogRepo, NullLogger<TaskExecutionService>.Instance);
        return (execService, repo, scanLogRepo);
    }

    [Fact]
    public async Task MarkScannedAsync_ShouldFail_WhenNoTaskFound()
    {
        var (service, _, _) = CreateService();
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

    [Fact]
    public async Task MarkScannedAsync_ShouldSucceed_WhenTaskIsCreated()
    {
        var (service, repo, _) = CreateService();
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
        Assert.Equal("已扫描", result.TaskStatus);
    }

    [Fact]
    public async Task MarkScannedAsync_ShouldSucceed_WhenTaskIsDropped()
    {
        var (service, repo, _) = CreateService();
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
        Assert.Equal("已扫描", result.TaskStatus);

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

    [Fact]
    public async Task MarkScannedAsync_ShouldPersistScannedStatus()
    {
        var (service, repo, _) = CreateService();
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

    [Fact]
    public async Task MarkScannedAsync_ShouldOverwriteTargetChuteCode_WhenRequestHasTargetChuteCode()
    {
        var (service, repo, _) = CreateService();
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

    [Fact]
    public async Task MarkScannedAsync_ShouldKeepTargetChuteCode_WhenRequestHasNoTargetChuteCode()
    {
        var (service, repo, _) = CreateService();
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

    [Fact]
    public async Task MarkScannedAsync_ShouldNotUpdateTargetChuteCode_WhenTaskStateIsInvalid()
    {
        var (service, repo, scanLogRepository) = CreateService();
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
        Assert.Single(scanLogRepository.Logs);
        Assert.True(scanLogRepository.Logs[0].IsMatched);
    }
}

