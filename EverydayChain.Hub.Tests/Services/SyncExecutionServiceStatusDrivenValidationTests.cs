using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Services;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// SyncExecutionService 状态驱动配置校验测试。
/// </summary>
public class SyncExecutionServiceStatusDrivenValidationTests
{
    /// <summary>
    /// 当 TargetLogicalTable 非 business_tasks 时应抛出配置异常。
    /// </summary>
    [Fact]
    public async Task ExecuteBatchAsync_ShouldThrow_WhenStatusDrivenTargetLogicalTableInvalid()
    {
        var service = CreateService();
        var context = CreateStatusDrivenContext(definition =>
        {
            definition.TargetLogicalTable = "sync_wms_status";
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteBatchAsync(context, CancellationToken.None));
        Assert.Contains("TargetLogicalTable 必须为 business_tasks", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 当 SourceType 为 Unknown 时应抛出配置异常。
    /// </summary>
    [Fact]
    public async Task ExecuteBatchAsync_ShouldThrow_WhenStatusDrivenSourceTypeUnknown()
    {
        var service = CreateService();
        var context = CreateStatusDrivenContext(definition =>
        {
            definition.SourceType = BusinessTaskSourceType.Unknown;
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteBatchAsync(context, CancellationToken.None));
        Assert.Contains("SourceType 不能为 Unknown", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 当 BusinessKeyColumn 为空白时应抛出配置异常。
    /// </summary>
    [Fact]
    public async Task ExecuteBatchAsync_ShouldThrow_WhenStatusDrivenBusinessKeyColumnBlank()
    {
        var service = CreateService();
        var context = CreateStatusDrivenContext(definition =>
        {
            definition.BusinessKeyColumn = "  ";
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteBatchAsync(context, CancellationToken.None));
        Assert.Contains("BusinessKeyColumn 不能为空白", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 创建待测服务。
    /// </summary>
    /// <returns>同步执行服务实例。</returns>
    private static SyncExecutionService CreateService()
    {
        return new SyncExecutionService(
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }

    /// <summary>
    /// 创建状态驱动执行上下文。
    /// </summary>
    /// <param name="configure">定义定制器。</param>
    /// <returns>执行上下文。</returns>
    private static SyncExecutionContext CreateStatusDrivenContext(Action<SyncTableDefinition> configure)
    {
        var definition = new SyncTableDefinition
        {
            TableCode = "WmsSplitPickToLightCarton",
            SyncMode = SyncMode.StatusDriven,
            TargetLogicalTable = "business_tasks",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKeyColumn = "CARTONNO",
        };
        configure(definition);
        return new SyncExecutionContext
        {
            BatchId = "batch-001",
            Definition = definition,
            Checkpoint = new SyncCheckpoint
            {
                TableCode = definition.TableCode,
            },
            Window = new SyncWindow(DateTime.Now.AddMinutes(-5), DateTime.Now),
        };
    }
}
