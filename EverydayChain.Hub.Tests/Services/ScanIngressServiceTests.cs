using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Services;
using EverydayChain.Hub.Application.TaskExecution.Services;
using EverydayChain.Hub.Application.ScanMatch.Services;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 扫描上传应用服务测试。
/// </summary>
public sealed class ScanIngressServiceTests
{
    /// <summary>
    /// 创建测试用的 ScanIngressService，注入内存替身依赖。
    /// </summary>
    /// <param name="repository">业务任务仓储替身。</param>
    /// <returns>扫描上传服务实例。</returns>
    private static ScanIngressService CreateService(IBusinessTaskRepository? repository = null)
    {
        var barcodeParser = new BarcodeParser();
        var repo = repository ?? new InMemoryBusinessTaskRepository();
        var matchService = new ScanMatchService(repo);
        var execService = new TaskExecutionService(matchService, repo);
        return new ScanIngressService(barcodeParser, execService);
    }

    /// <summary>
    /// 无效条码应返回统一失败语义。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldReturnInvalidBarcode_WhenBarcodeIsInvalid()
    {
        var service = CreateService();
        var request = new ScanUploadApplicationRequest
        {
            Barcode = " ",
            DeviceCode = "DVC-01",
            ScanTimeLocal = DateTime.Now
        };

        var result = await service.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.IsAccepted);
        Assert.Equal("Unknown", result.BarcodeType);
        Assert.Equal("InvalidBarcode", result.FailureReason);
    }

    /// <summary>
    /// 有效拆零条码但无匹配任务时应返回未命中结果。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotMatched_WhenNoTaskFound()
    {
        var service = CreateService();
        var request = new ScanUploadApplicationRequest
        {
            Barcode = "S-ABC001",
            DeviceCode = "DVC-01",
            ScanTimeLocal = DateTime.Now
        };

        var result = await service.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.IsAccepted);
        Assert.Equal("Split", result.BarcodeType);
        Assert.Equal("TaskNotMatchedOrInvalidState", result.FailureReason);
    }

    /// <summary>
    /// 有效拆零条码且有匹配任务时应返回受理结果与条码类型。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldAcceptRequest_WhenBarcodeIsSplitAndTaskExists()
    {
        var repo = new InMemoryBusinessTaskRepository();
        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-001",
            SourceTableCode = "WMS",
            BusinessKey = "KEY-001",
            Barcode = "S-ABC001",
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        var service = CreateService(repo);
        var request = new ScanUploadApplicationRequest
        {
            Barcode = "S-ABC001",
            DeviceCode = "DVC-01",
            ScanTimeLocal = DateTime.Now
        };

        var result = await service.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.IsAccepted);
        Assert.Equal("Split", result.BarcodeType);
        Assert.Equal(string.Empty, result.FailureReason);
        Assert.Equal("TASK-001", result.TaskCode);
    }
}
