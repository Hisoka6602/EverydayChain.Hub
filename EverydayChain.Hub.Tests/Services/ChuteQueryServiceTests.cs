using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Services;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 ChuteQueryServiceTests 类型。
/// </summary>
public sealed class ChuteQueryServiceTests
{
    private static (ChuteQueryService Service, InMemoryBusinessTaskRepository Repository) CreateService()
    {
        var repo = new InMemoryBusinessTaskRepository();
        var service = new ChuteQueryService(repo, new BarcodeParser());
        return (service, repo);
    }

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

    [Fact]
    public async Task ExecuteAsync_ShouldSucceed_WhenTaskIsDroppedAndBarcodeCanResolveChute()
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

    [Fact]
    public async Task ExecuteAsync_ShouldUsePersistedTargetChuteCode_WhenTargetChuteCodeExists()
    {
        var (service, repo) = CreateService();
        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-006",
            SourceTableCode = "WMS",
            BusinessKey = "K6",
            Barcode = "INVALID-BARCODE",
            TargetChuteCode = " 6 ",
            Status = BusinessTaskStatus.Scanned,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        var result = await service.ExecuteAsync(new ChuteResolveApplicationRequest
        {
            TaskCode = "TASK-006"
        }, CancellationToken.None);

        Assert.True(result.IsResolved);
        Assert.Equal("TASK-006", result.TaskCode);
        Assert.Equal("6", result.ChuteCode);
    }
}

