using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Services;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 请求格口服务测试（接入真实仓储替身）。
/// </summary>
public sealed class ChuteQueryServiceTests
{
    /// <summary>
    /// 构建测试用的 ChuteQueryService。
    /// </summary>
    private static (ChuteQueryService Service, InMemoryBusinessTaskRepository Repository) CreateService()
    {
        var repo = new InMemoryBusinessTaskRepository();
        var service = new ChuteQueryService(repo);
        return (service, repo);
    }

    /// <summary>
    /// 任务不存在时应返回失败结果。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenTaskNotFound()
    {
        var (service, _) = CreateService();
        var request = new ChuteResolveApplicationRequest
        {
            Barcode = "UNKNOWN",
            TaskCode = string.Empty
        };

        var result = await service.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.IsResolved);
        Assert.Empty(result.ChuteCode);
    }

    /// <summary>
    /// 任务状态不是已扫描时应返回失败结果。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenTaskIsNotScanned()
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

        var request = new ChuteResolveApplicationRequest { Barcode = "BC-001" };

        var result = await service.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.IsResolved);
        Assert.Equal("TASK-001", result.TaskCode);
        Assert.Contains("Created", result.Message);
    }

    /// <summary>
    /// 任务已扫描但未分配格口时应返回失败结果。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenTargetChuteCodeIsEmpty()
    {
        var (service, repo) = CreateService();
        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-002",
            SourceTableCode = "WMS",
            BusinessKey = "K2",
            Barcode = "BC-002",
            Status = BusinessTaskStatus.Scanned,
            TargetChuteCode = null,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        var request = new ChuteResolveApplicationRequest { Barcode = "BC-002" };

        var result = await service.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.IsResolved);
        Assert.Contains("尚未分配目标格口", result.Message);
    }

    /// <summary>
    /// 任务已扫描且有目标格口时应返回成功结果。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldSucceed_WhenTaskIsScannedWithChute()
    {
        var (service, repo) = CreateService();
        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-003",
            SourceTableCode = "WMS",
            BusinessKey = "K3",
            Barcode = "BC-003",
            Status = BusinessTaskStatus.Scanned,
            TargetChuteCode = "CHUTE-A1",
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        var request = new ChuteResolveApplicationRequest { Barcode = "BC-003" };

        var result = await service.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.IsResolved);
        Assert.Equal("TASK-003", result.TaskCode);
        Assert.Equal("CHUTE-A1", result.ChuteCode);
    }

    /// <summary>
    /// 按任务编码查找优先于条码查找。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldPreferTaskCodeOverBarcode()
    {
        var (service, repo) = CreateService();
        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-004",
            SourceTableCode = "WMS",
            BusinessKey = "K4",
            Barcode = "BC-004",
            Status = BusinessTaskStatus.Scanned,
            TargetChuteCode = "CHUTE-B2",
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        var request = new ChuteResolveApplicationRequest
        {
            TaskCode = "TASK-004",
            Barcode = "WRONG-BC"
        };

        var result = await service.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.IsResolved);
        Assert.Equal("TASK-004", result.TaskCode);
        Assert.Equal("CHUTE-B2", result.ChuteCode);
    }
}
