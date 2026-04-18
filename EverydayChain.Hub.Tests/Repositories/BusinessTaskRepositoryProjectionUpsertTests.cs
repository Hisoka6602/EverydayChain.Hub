using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Tests.Services;

namespace EverydayChain.Hub.Tests.Repositories;

/// <summary>
/// 业务任务投影幂等 Upsert 测试。
/// </summary>
public class BusinessTaskRepositoryProjectionUpsertTests
{
    /// <summary>
    /// 相同来源表与业务键重复投影时应执行幂等更新，不新增重复数据。
    /// </summary>
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
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        var found = await repository.FindBySourceTableAndBusinessKeyAsync("WmsSplitPickToLightCarton", "BK-1", CancellationToken.None);
        Assert.NotNull(found);
        Assert.Equal("W2", found!.WaveCode);
        Assert.Equal("R2", found.WaveRemark);
        Assert.Equal("BK-1", found.TaskCode);
    }

    /// <summary>
    /// 已进入运行态时重复投影不得覆盖运行态字段。
    /// </summary>
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
