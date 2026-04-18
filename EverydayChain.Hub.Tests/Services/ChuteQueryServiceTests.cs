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
        var service = new ChuteQueryService(repo, new BarcodeParser());
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
    /// 任务已扫描但条码未携带格口时应返回失败结果。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenBarcodeHasNoChuteInfo()
    {
        var (service, repo) = CreateService();
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

        var request = new ChuteResolveApplicationRequest { Barcode = "BC-002" };

        var result = await service.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.IsResolved);
        Assert.Contains("未携带受支持的目标格口信息", result.Message);
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
            Barcode = "021103013145",
            Status = BusinessTaskStatus.Scanned,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        var request = new ChuteResolveApplicationRequest { Barcode = "021103013145" };

        var result = await service.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.IsResolved);
        Assert.Equal("TASK-003", result.TaskCode);
        Assert.Equal("1", result.ChuteCode);
    }

    /// <summary>
    /// 任务已落格且有目标格口时应支持重复请求并返回成功结果。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldSucceed_WhenTaskIsDroppedWithChute()
    {
        var (service, repo) = CreateService();
        var now = new DateTime(2026, 4, 18, 10, 0, 0, DateTimeKind.Local);
        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-005",
            SourceTableCode = "WMS",
            BusinessKey = "K5",
            Barcode = "021103013145",
            Status = BusinessTaskStatus.Dropped,
            CreatedTimeLocal = now,
            UpdatedTimeLocal = now
        }, CancellationToken.None);

        var request = new ChuteResolveApplicationRequest { Barcode = "021103013145" };

        var result = await service.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.IsResolved);
        Assert.Equal("TASK-005", result.TaskCode);
        Assert.Equal("1", result.ChuteCode);
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
            Barcode = "Z46030202060001",
            Status = BusinessTaskStatus.Scanned,
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
        Assert.Equal("4", result.ChuteCode);
    }
}
