using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Domain.Sync.Models;
using EverydayChain.Hub.Infrastructure.Sync.Services;
using EverydayChain.Hub.Tests.Services;
using EverydayChain.Hub.Tests.Sync.Fakes;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Tests.Sync;

/// <summary>
/// RemoteStatusConsumeService 行为测试。
/// </summary>
public class RemoteStatusConsumeServiceTests
{
    /// <summary>
    /// 启用回写时应统计读取、追加、回写与缺失 RowId 跳过计数。
    /// </summary>
    [Fact]
    public async Task ConsumeAsync_WhenWriteBackEnabled_ShouldAggregateCounts()
    {
        var reader = new FakeOracleStatusDrivenSourceReader();
        reader.Pages.Enqueue([
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["ID"] = 1,
                ["TASKPROCESS"] = "N",
                ["__RowId"] = "AAABBB",
            },
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["ID"] = 2,
                ["TASKPROCESS"] = "N",
                ["__RowId"] = null,
            },
        ]);
        reader.Pages.Enqueue([]);

        var appendWriter = new FakeSqlServerAppendOnlyWriter();
        var remoteWriter = new FakeOracleRemoteStatusWriter();
        var logger = new TestLogger<RemoteStatusConsumeService>();
        var service = new RemoteStatusConsumeService(reader, appendWriter, remoteWriter, logger);
        var definition = BuildStatusDrivenDefinition();

        var result = await service.ConsumeAsync(definition, "BATCH-001", default, CancellationToken.None);

        Assert.Equal(2, result.ReadCount);
        Assert.Equal(2, result.AppendCount);
        Assert.Equal(1, result.WriteBackCount);
        Assert.Equal(0, result.WriteBackFailCount);
        Assert.Equal(1, result.SkippedWriteBackCount);
        Assert.Equal(1, result.PageCount);
        Assert.Equal(2, appendWriter.TotalAppended);
        Assert.Equal(1, remoteWriter.TotalWriteBackRows);
        Assert.Equal("BATCH-001", remoteWriter.LastBatchId);
    }

    /// <summary>
    /// 启用回写时应固定读取第 1 页，避免集合收缩导致翻页跳过。
    /// </summary>
    [Fact]
    public async Task ConsumeAsync_WhenWriteBackEnabled_ShouldAlwaysReadFirstPage()
    {
        var reader = new FakeOracleStatusDrivenSourceReader();
        reader.Pages.Enqueue([
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["ID"] = 1,
                ["TASKPROCESS"] = "N",
                ["__RowId"] = "AAA001",
            },
        ]);
        reader.Pages.Enqueue([]);
        var appendWriter = new FakeSqlServerAppendOnlyWriter();
        var remoteWriter = new FakeOracleRemoteStatusWriter();
        var logger = new TestLogger<RemoteStatusConsumeService>();
        var service = new RemoteStatusConsumeService(reader, appendWriter, remoteWriter, logger);
        var definition = BuildStatusDrivenDefinition();

        var result = await service.ConsumeAsync(definition, "BATCH-002", default, CancellationToken.None);

        Assert.Equal([1, 1], reader.RequestedPageNos);
        Assert.Equal(1, result.AppendCount);
        Assert.Equal(1, result.WriteBackCount);
        Assert.Equal("BATCH-002", remoteWriter.LastBatchId);
    }

    /// <summary>
    /// 关闭回写时应仅追加本地数据，不触发任何远端状态更新。
    /// </summary>
    [Fact]
    public async Task ConsumeAsync_WhenWriteBackDisabled_ShouldAppendOnlyWithoutRemoteUpdate()
    {
        var reader = new FakeOracleStatusDrivenSourceReader();
        reader.Pages.Enqueue([
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["ID"] = 1,
                ["TASKPROCESS"] = "N",
                ["__RowId"] = "AAABBB",
            },
        ]);
        reader.Pages.Enqueue([]);

        var appendWriter = new FakeSqlServerAppendOnlyWriter();
        var remoteWriter = new FakeOracleRemoteStatusWriter();
        var logger = new TestLogger<RemoteStatusConsumeService>();
        var service = new RemoteStatusConsumeService(reader, appendWriter, remoteWriter, logger);
        var definition = BuildStatusDrivenDefinitionWithWriteBackDisabled();

        var result = await service.ConsumeAsync(definition, "BATCH-003", default, CancellationToken.None);

        Assert.Equal(1, result.ReadCount);
        Assert.Equal(1, result.AppendCount);
        Assert.Equal(0, result.WriteBackCount);
        Assert.Equal(0, result.WriteBackFailCount);
        Assert.Equal(0, result.SkippedWriteBackCount);
        Assert.Equal(1, result.PageCount);
        Assert.Equal(1, appendWriter.TotalAppended);
        Assert.Equal(0, remoteWriter.TotalWriteBackRows);
    }

    /// <summary>
    /// 回写异常时应记录错误日志并写出失败原因。
    /// </summary>
    [Fact]
    public async Task ConsumeAsync_WhenWriteBackThrows_ShouldLogFailureReason()
    {
        var reader = new FakeOracleStatusDrivenSourceReader();
        reader.Pages.Enqueue([
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["ID"] = 1,
                ["TASKPROCESS"] = "N",
                ["__RowId"] = "ROW001",
            },
        ]);
        var appendWriter = new FakeSqlServerAppendOnlyWriter();
        var remoteWriter = new FakeOracleRemoteStatusWriter
        {
            ThrowOnWriteBack = true,
            ThrowMessage = "模拟回写失败：网络中断",
        };
        var logger = new TestLogger<RemoteStatusConsumeService>();
        var service = new RemoteStatusConsumeService(reader, appendWriter, remoteWriter, logger);
        var definition = BuildStatusDrivenDefinition();

        var result = await service.ConsumeAsync(definition, "BATCH-004", default, CancellationToken.None);

        Assert.Equal(1, result.ReadCount);
        Assert.Equal(1, result.AppendCount);
        Assert.Equal(0, result.WriteBackCount);
        Assert.Equal(1, result.WriteBackFailCount);
        Assert.Contains(
            logger.Logs,
            x => x.Level == LogLevel.Error
                 && x.Message.Contains("FailureReason=模拟回写失败：网络中断", StringComparison.Ordinal));
    }

    /// <summary>
    /// 构建状态驱动测试定义。
    /// </summary>
    /// <returns>同步表定义。</returns>
    private static SyncTableDefinition BuildStatusDrivenDefinition()
    {
        return new SyncTableDefinition
        {
            TableCode = "T1",
            Enabled = true,
            SyncMode = SyncMode.StatusDriven,
            SourceSchema = "SRC",
            SourceTable = "TAB1",
            TargetLogicalTable = "TAB1",
            StatusConsumeProfile = new RemoteStatusConsumeProfile
            {
                StatusColumnName = "TASKPROCESS",
                PendingStatusValue = "N",
                CompletedStatusValue = "Y",
                ShouldWriteBackRemoteStatus = true,
                BatchSize = 5000,
            },
        };
    }

    /// <summary>
    /// 构建关闭回写的状态驱动测试定义。
    /// </summary>
    /// <returns>同步表定义。</returns>
    private static SyncTableDefinition BuildStatusDrivenDefinitionWithWriteBackDisabled()
    {
        return new SyncTableDefinition
        {
            TableCode = "T1",
            Enabled = true,
            SyncMode = SyncMode.StatusDriven,
            SourceSchema = "SRC",
            SourceTable = "TAB1",
            TargetLogicalTable = "TAB1",
            StatusConsumeProfile = new RemoteStatusConsumeProfile
            {
                StatusColumnName = "TASKPROCESS",
                PendingStatusValue = "N",
                CompletedStatusValue = "Y",
                ShouldWriteBackRemoteStatus = false,
                BatchSize = 5000,
            },
        };
    }
}
