using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Tests.Services;

namespace EverydayChain.Hub.Tests.Repositories;

/// <summary>
/// 定义 BusinessTaskRepositoryProjectionUpsertTests 类型。
/// </summary>
public class BusinessTaskRepositoryProjectionUpsertTests
{
    [Fact]
    public async Task UpsertProjectionAsync_WhenSameSourceAndBusinessKey_ShouldBeIdempotent()
    {
        var repository = new InMemoryBusinessTaskRepository();
        var created = DateTime.Now.AddMinutes(-5);
        await repository.UpsertProjectionAsync(new BusinessTaskEntity
        {
            TaskCode = "BK-1",
            SourceTableCode = "WmsSplitPickToLightCarton",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "BK-1",
            Barcode = "BK-1",
            WaveCode = "W1",
            WaveRemark = "R1",
            WorkingArea = "1",
            OrderId = "ORDER-1",
            StoreId = "STORE-1",
            StoreName = "Store One",
            ProductCode = "SKU-1",
            PickLocation = "LOC-1",
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = created,
            UpdatedTimeLocal = created
        }, CancellationToken.None);

        await repository.UpsertProjectionAsync(new BusinessTaskEntity
        {
            TaskCode = "BK-1",
            SourceTableCode = "WmsSplitPickToLightCarton",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "BK-1",
            Barcode = "BK-1-NEW",
            WaveCode = "W2",
            WaveRemark = "R2",
            WorkingArea = "2",
            OrderId = "ORDER-2",
            StoreId = "STORE-2",
            StoreName = "Store Two",
            ProductCode = "SKU-2",
            PickLocation = "LOC-2",
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        var found = await repository.FindBySourceTableAndBusinessKeyAsync("WmsSplitPickToLightCarton", "BK-1", CancellationToken.None);
        Assert.NotNull(found);
        Assert.Equal("W2", found!.WaveCode);
        Assert.Equal("R2", found.WaveRemark);
        Assert.Equal("2", found.WorkingArea);
        Assert.Equal("BK-1", found.TaskCode);
        Assert.Equal("ORDER-2", found.OrderId);
        Assert.Equal("STORE-2", found.StoreId);
        Assert.Equal("Store Two", found.StoreName);
        Assert.Equal("SKU-2", found.ProductCode);
        Assert.Equal("LOC-2", found.PickLocation);
    }

    [Fact]
    public async Task UpsertProjectionAsync_WhenEntityInRuntimeState_ShouldNotOverwriteRuntimeFields()
    {
        var repository = new InMemoryBusinessTaskRepository();
        var created = DateTime.Now.AddMinutes(-10);
        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "BK-2",
            SourceTableCode = "WmsSplitPickToLightCarton",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "BK-2",
            Barcode = "BK-2",
            TargetChuteCode = "A01",
            Status = BusinessTaskStatus.Scanned,
            ScannedAtLocal = DateTime.Now.AddMinutes(-1),
            CreatedTimeLocal = created,
            UpdatedTimeLocal = created
        }, CancellationToken.None);

        await repository.UpsertProjectionAsync(new BusinessTaskEntity
        {
            TaskCode = "BK-2",
            SourceTableCode = "WmsSplitPickToLightCarton",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "BK-2",
            Barcode = "BK-2-NEW",
            WaveCode = "W3",
            WaveRemark = "R3",
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        var found = await repository.FindBySourceTableAndBusinessKeyAsync("WmsSplitPickToLightCarton", "BK-2", CancellationToken.None);
        Assert.NotNull(found);
        Assert.Equal(BusinessTaskStatus.Scanned, found!.Status);
        Assert.Equal("A01", found.TargetChuteCode);
    }
}

