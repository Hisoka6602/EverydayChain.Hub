using EverydayChain.Hub.Application.ScanMatch.Services;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 扫描匹配服务单元测试。
/// </summary>
public sealed class ScanMatchServiceTests
{
    /// <summary>
    /// 条码为空白时应返回未匹配结果。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MatchByBarcodeAsync_ShouldReturnNotMatched_WhenBarcodeIsBlank(string barcode)
    {
        var repo = new InMemoryBusinessTaskRepository();
        var service = new ScanMatchService(repo);

        var result = await service.MatchByBarcodeAsync(barcode, CancellationToken.None);

        Assert.False(result.IsMatched);
        Assert.Null(result.Task);
        Assert.NotEmpty(result.FailureReason);
    }

    /// <summary>
    /// 条码无对应任务时应返回未匹配结果。
    /// </summary>
    [Fact]
    public async Task MatchByBarcodeAsync_ShouldReturnNotMatched_WhenNoTaskExists()
    {
        var repo = new InMemoryBusinessTaskRepository();
        var service = new ScanMatchService(repo);

        var result = await service.MatchByBarcodeAsync("UNKNOWN-BC", CancellationToken.None);

        Assert.False(result.IsMatched);
        Assert.Null(result.Task);
        Assert.Contains("UNKNOWN-BC", result.FailureReason);
    }

    /// <summary>
    /// 条码有对应任务时应返回匹配成功与任务实体。
    /// </summary>
    [Fact]
    public async Task MatchByBarcodeAsync_ShouldReturnMatched_WhenTaskExists()
    {
        var repo = new InMemoryBusinessTaskRepository();
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

        var service = new ScanMatchService(repo);

        var result = await service.MatchByBarcodeAsync("BC-001", CancellationToken.None);

        Assert.True(result.IsMatched);
        Assert.NotNull(result.Task);
        Assert.Equal("TASK-001", result.Task!.TaskCode);
        Assert.Empty(result.FailureReason);
    }
}
