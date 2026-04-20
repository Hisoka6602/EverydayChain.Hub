using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 业务任务模拟补数应用服务测试。
/// </summary>
public sealed class BusinessTaskSeedServiceTests
{
    /// <summary>
    /// 空条码集合应失败。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenBarcodeCollectionIsEmpty()
    {
        var service = new BusinessTaskSeedService(new StubBusinessTaskSeedRepository(), NullLogger<BusinessTaskSeedService>.Instance);

        var result = await service.ExecuteAsync(new BusinessTaskSeedCommand
        {
            TargetTableName = "business_tasks_202604",
            Barcodes = []
        }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("条码集合不能为空。", result.Message);
    }

    /// <summary>
    /// 全空白条码应失败。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenAllBarcodesAreWhitespace()
    {
        var service = new BusinessTaskSeedService(new StubBusinessTaskSeedRepository(), NullLogger<BusinessTaskSeedService>.Instance);

        var result = await service.ExecuteAsync(new BusinessTaskSeedCommand
        {
            TargetTableName = "business_tasks_202604",
            Barcodes = [" ", "\t", "  "]
        }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("条码清洗后为空，请至少提供一个有效条码。", result.Message);
    }

    /// <summary>
    /// 请求内重复条码应去重并统计。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldDeduplicate_WhenRequestContainsDuplicates()
    {
        var repository = new StubBusinessTaskSeedRepository();
        var service = new BusinessTaskSeedService(repository, NullLogger<BusinessTaskSeedService>.Instance);

        var result = await service.ExecuteAsync(new BusinessTaskSeedCommand
        {
            TargetTableName = "business_tasks_202604",
            Barcodes = ["BC001", "BC001", "BC002"]
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.DeduplicatedCount);
        Assert.NotNull(repository.LastCommand);
        Assert.Equal(["BC001", "BC002"], repository.LastCommand!.Barcodes);
    }

    /// <summary>
    /// 去重后应保留首次出现顺序。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldKeepInputOrder_AfterDeduplication()
    {
        var repository = new StubBusinessTaskSeedRepository();
        var service = new BusinessTaskSeedService(repository, NullLogger<BusinessTaskSeedService>.Instance);

        var result = await service.ExecuteAsync(new BusinessTaskSeedCommand
        {
            TargetTableName = "business_tasks_202604",
            Barcodes = ["BC003", " BC001 ", "BC003", "BC002"]
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.LastCommand);
        Assert.Equal(["BC003", "BC001", "BC002"], repository.LastCommand!.Barcodes);
    }

    /// <summary>
    /// 仓储返回条码明细应原样透传。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldPassThroughBarcodeDetails_FromRepositoryResult()
    {
        var repository = new StubBusinessTaskSeedRepository
        {
            ResultFactory = command => new BusinessTaskSeedResult
            {
                IsSuccess = true,
                Message = "模拟补数写入成功。",
                TargetTableName = command.TargetTableName,
                InsertedCount = 1,
                SkippedExistingCount = 1,
                InsertedBarcodes = ["BC001"],
                SkippedBarcodes = ["BC002"]
            }
        };
        var service = new BusinessTaskSeedService(repository, NullLogger<BusinessTaskSeedService>.Instance);

        var result = await service.ExecuteAsync(new BusinessTaskSeedCommand
        {
            TargetTableName = " business_tasks_202604 ",
            Barcodes = ["BC001", "BC002"]
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["BC001"], result.InsertedBarcodes);
        Assert.Equal(["BC002"], result.SkippedBarcodes);
    }

    /// <summary>
    /// 超出最大条码数量应失败。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenBarcodesExceedLimit()
    {
        var service = new BusinessTaskSeedService(new StubBusinessTaskSeedRepository(), NullLogger<BusinessTaskSeedService>.Instance);

        var result = await service.ExecuteAsync(new BusinessTaskSeedCommand
        {
            TargetTableName = "business_tasks_202604",
            Barcodes = Enumerable.Repeat("BC001", 5001).ToArray()
        }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("单次最多允许提交 5000 个条码。", result.Message);
    }
}
