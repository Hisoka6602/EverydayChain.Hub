using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.ScanMatch.Services;
using EverydayChain.Hub.Application.Services;
using EverydayChain.Hub.Application.TaskExecution.Services;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 扫描日志与落格日志落库行为测试。
/// </summary>
public sealed class ScanDropLogTests
{
    /// <summary>
    /// 扫描匹配成功时应写入匹配成功的扫描日志。
    /// </summary>
    [Fact]
    public async Task MarkScannedAsync_ShouldWriteScanLog_OnSuccess()
    {
        var repo = new InMemoryBusinessTaskRepository();
        var scanLogRepo = new InMemoryScanLogRepository();
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

        var matchService = new ScanMatchService(repo);
        var service = new TaskExecutionService(matchService, repo, scanLogRepo, NullLogger<TaskExecutionService>.Instance);
        var request = new ScanUploadApplicationRequest
        {
            Barcode = "BC-001",
            DeviceCode = "DVC-01",
            ScanTimeLocal = DateTime.Now
        };

        var result = await service.MarkScannedAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(scanLogRepo.Logs);
        var log = scanLogRepo.Logs[0];
        Assert.True(log.IsMatched);
        Assert.Equal("BC-001", log.Barcode);
        Assert.Equal("DVC-01", log.DeviceCode);
    }

    /// <summary>
    /// 扫描匹配失败时应写入匹配失败的扫描日志。
    /// </summary>
    [Fact]
    public async Task MarkScannedAsync_ShouldWriteScanLog_OnFailure()
    {
        var repo = new InMemoryBusinessTaskRepository();
        var scanLogRepo = new InMemoryScanLogRepository();

        var matchService = new ScanMatchService(repo);
        var service = new TaskExecutionService(matchService, repo, scanLogRepo, NullLogger<TaskExecutionService>.Instance);
        var request = new ScanUploadApplicationRequest
        {
            Barcode = "UNKNOWN-BC",
            ScanTimeLocal = DateTime.Now
        };

        var result = await service.MarkScannedAsync(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Single(scanLogRepo.Logs);
        var log = scanLogRepo.Logs[0];
        Assert.False(log.IsMatched);
        Assert.Equal("UNKNOWN-BC", log.Barcode);
        Assert.NotEmpty(log.FailureReason!);
    }

    /// <summary>
    /// 落格成功时应写入落格成功的落格日志，且任务 FeedbackStatus 应为 Pending。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldWriteDropLog_AndSetFeedbackPending_OnSuccess()
    {
        var repo = new InMemoryBusinessTaskRepository();
        var dropLogRepo = new InMemoryDropLogRepository();
        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-002",
            SourceTableCode = "WMS",
            BusinessKey = "K2",
            Barcode = "BC-002",
            Status = BusinessTaskStatus.Scanned,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        var service = new DropFeedbackService(repo, dropLogRepo, NullLogger<DropFeedbackService>.Instance);
        var request = new DropFeedbackApplicationRequest
        {
            Barcode = "BC-002",
            ActualChuteCode = "CHUTE-A1",
            DropTimeLocal = DateTime.Now,
            IsSuccess = true
        };

        var result = await service.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.IsAccepted);
        Assert.Single(dropLogRepo.Logs);
        var log = dropLogRepo.Logs[0];
        Assert.True(log.IsSuccess);
        Assert.Equal("CHUTE-A1", log.ActualChuteCode);

        var updated = await repo.FindByTaskCodeAsync("TASK-002", CancellationToken.None);
        Assert.Equal(BusinessTaskFeedbackStatus.Pending, updated!.FeedbackStatus);
    }

    /// <summary>
    /// 落格失败时应写入落格失败的落格日志。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldWriteDropLog_OnFailure()
    {
        var repo = new InMemoryBusinessTaskRepository();
        var dropLogRepo = new InMemoryDropLogRepository();
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

        var service = new DropFeedbackService(repo, dropLogRepo, NullLogger<DropFeedbackService>.Instance);
        var request = new DropFeedbackApplicationRequest
        {
            Barcode = "BC-003",
            IsSuccess = false,
            FailureReason = "分拣机故障"
        };

        var result = await service.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.IsAccepted);
        Assert.Single(dropLogRepo.Logs);
        var log = dropLogRepo.Logs[0];
        Assert.False(log.IsSuccess);
        Assert.Equal("分拣机故障", log.FailureReason);
    }
}
