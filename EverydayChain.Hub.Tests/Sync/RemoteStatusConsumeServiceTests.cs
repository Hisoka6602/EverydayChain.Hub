using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Domain.Sync.Models;
using EverydayChain.Hub.Infrastructure.Sync.Services;
using EverydayChain.Hub.Tests.Services;
using EverydayChain.Hub.Tests.Sync.Fakes;

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

        var result = await service.ConsumeAsync(definition, "BATCH-001", CancellationToken.None);

        Assert.Equal(2, result.ReadCount);
        Assert.Equal(2, result.AppendCount);
        Assert.Equal(1, result.WriteBackCount);
        Assert.Equal(0, result.WriteBackFailCount);
        Assert.Equal(1, result.SkippedWriteBackCount);
        Assert.Equal(1, result.PageCount);
        Assert.Equal(2, appendWriter.TotalAppended);
        Assert.Equal(1, remoteWriter.TotalWriteBackRows);
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
}
