using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.ScanMatch.Services;
using EverydayChain.Hub.Application.TaskExecution.Services;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;

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
        var matchService = new ScanMatchService(repo);
        var execService = new TaskExecutionService(matchService, repo);
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
    /// 任务处于已落格或更高状态时应拒绝推进并返回失败。
    /// </summary>
    [Fact]
    public async Task MarkScannedAsync_ShouldFail_WhenTaskIsDropped()
    {
        var (service, repo) = CreateService();
        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-002",
            SourceTableCode = "WMS",
            BusinessKey = "K2",
            Barcode = "BC-002",
            Status = BusinessTaskStatus.Dropped,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        var request = new ScanUploadApplicationRequest
        {
            Barcode = "BC-002",
            ScanTimeLocal = DateTime.Now
        };

        var result = await service.MarkScannedAsync(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Dropped", result.FailureReason);
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
            ScanTimeLocal = new DateTime(2026, 4, 13, 10, 0, 0, DateTimeKind.Local)
        };

        await service.MarkScannedAsync(request, CancellationToken.None);

        var updatedTask = await repo.FindByTaskCodeAsync("TASK-003", CancellationToken.None);
        Assert.NotNull(updatedTask);
        Assert.Equal(BusinessTaskStatus.Scanned, updatedTask!.Status);
        Assert.Equal("DVC-02", updatedTask.DeviceCode);
        Assert.Equal(new DateTime(2026, 4, 13, 10, 0, 0, DateTimeKind.Local), updatedTask.ScannedAtLocal);
    }
}
