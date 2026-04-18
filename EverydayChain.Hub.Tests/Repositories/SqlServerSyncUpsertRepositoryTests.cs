using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Repositories;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EverydayChain.Hub.Tests.Services;
using EverydayChain.Hub.Infrastructure.Services;

namespace EverydayChain.Hub.Tests.Repositories;

/// <summary>
/// SqlServerSyncUpsertRepository 合并行为测试。
/// </summary>
public class SqlServerSyncUpsertRepositoryTests
{
    /// <summary>
    /// 新增行应计入插入统计。
    /// </summary>
    [Fact]
    public async Task MergeFromStagingAsync_WhenNewRow_ShouldInsert()
    {
        var repository = CreateRepository();
        var result = await repository.MergeFromStagingAsync(CreateRequest("BK1", "A"), CancellationToken.None);

        Assert.Equal(1, result.InsertCount);
        Assert.Equal(0, result.UpdateCount);
        Assert.Equal(0, result.SkipCount);
    }

    /// <summary>
    /// 摘要变化应计入更新统计。
    /// </summary>
    [Fact]
    public async Task MergeFromStagingAsync_WhenDigestChanged_ShouldUpdate()
    {
        var repository = CreateRepository();
        await repository.MergeFromStagingAsync(CreateRequest("BK1", "A"), CancellationToken.None);
        var result = await repository.MergeFromStagingAsync(CreateRequest("BK1", "B"), CancellationToken.None);

        Assert.Equal(0, result.InsertCount);
        Assert.Equal(1, result.UpdateCount);
        Assert.Equal(0, result.SkipCount);
    }

    /// <summary>
    /// 摘要一致应计入跳过统计。
    /// </summary>
    [Fact]
    public async Task MergeFromStagingAsync_WhenDigestUnchanged_ShouldSkip()
    {
        var repository = CreateRepository();
        await repository.MergeFromStagingAsync(CreateRequest("BK1", "A"), CancellationToken.None);
        var result = await repository.MergeFromStagingAsync(CreateRequest("BK1", "A"), CancellationToken.None);

        Assert.Equal(0, result.InsertCount);
        Assert.Equal(0, result.UpdateCount);
        Assert.Equal(1, result.SkipCount);
    }

    /// <summary>
    /// 批量混合变更应返回正确插入/更新/跳过统计。
    /// </summary>
    [Fact]
    public async Task MergeFromStagingAsync_WhenBatchContainsInsertUpdateSkip_ShouldReturnExpectedCounts()
    {
        var repository = CreateRepository();
        await repository.MergeFromStagingAsync(CreateRequest("BK1", "A"), CancellationToken.None);
        await repository.MergeFromStagingAsync(CreateRequest("BK2", "A"), CancellationToken.None);
        var result = await repository.MergeFromStagingAsync(CreateRequest(
            ("BK1", "A", new DateTime(2026, 4, 7, 10, 0, 0, DateTimeKind.Local)),
            ("BK2", "B", new DateTime(2026, 4, 7, 10, 0, 0, DateTimeKind.Local)),
            ("BK3", "C", new DateTime(2026, 4, 7, 10, 0, 0, DateTimeKind.Local))), CancellationToken.None);

        Assert.Equal(1, result.InsertCount);
        Assert.Equal(1, result.UpdateCount);
        Assert.Equal(1, result.SkipCount);
        Assert.Equal(SyncChangeOperationType.Insert, result.ChangedOperations["BK3"]);
        Assert.Equal(SyncChangeOperationType.Update, result.ChangedOperations["BK2"]);
    }

    /// <summary>
    /// 分片切换时应删除旧分片并写入新分片。
    /// </summary>
    [Fact]
    public async Task MergeFromStagingAsync_WhenShardSwitched_ShouldDeleteOldShardAndWriteNewShard()
    {
        var repository = CreateRepository();
        await repository.MergeFromStagingAsync(CreateRequest("BK1", "A", new DateTime(2026, 4, 7, 10, 0, 0, DateTimeKind.Local)), CancellationToken.None);
        var result = await repository.MergeFromStagingAsync(CreateRequest("BK1", "B", new DateTime(2026, 5, 7, 10, 0, 0, DateTimeKind.Local)), CancellationToken.None);

        Assert.Equal(0, result.InsertCount);
        Assert.Equal(1, result.UpdateCount);
        Assert.Equal(0, result.SkipCount);
        Assert.False(repository.ExistsInShard("BK1", "_202604"));
        Assert.True(repository.ExistsInShard("BK1", "_202605"));
        Assert.Equal(1, repository.ShardMigrationDeleteCount);
    }

    /// <summary>
    /// 未配置唯一键应抛异常。
    /// </summary>
    [Fact]
    public async Task MergeFromStagingAsync_WhenUniqueKeysMissing_ShouldThrow()
    {
        var repository = CreateRepository();
        var request = new SyncMergeRequest
        {
            TableCode = "WmsSplitPickToLightCarton",
            CursorColumn = "ADDTIME",
            UniqueKeys = Array.Empty<string>(),
            Rows =
            [
                new Dictionary<string, object?>
                {
                    ["CARTONNO"] = "BK1",
                    ["PAYLOAD"] = "A"
                }
            ]
        };

        var action = () => repository.MergeFromStagingAsync(request, CancellationToken.None);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(action);
        Assert.Contains("UniqueKeys", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 业务键构建应对时间字段使用稳定本地时间格式。
    /// </summary>
    [Fact]
    public void BuildBusinessKey_WhenContainsDateTime_ShouldUseInvariantLocalFormat()
    {
        var row = new Dictionary<string, object?>
        {
            ["DOCNO"] = "D001",
            ["ADDTIME"] = new DateTime(2026, 4, 8, 10, 11, 12, 123, DateTimeKind.Local).AddTicks(4567)
        };

        var businessKey = SyncBusinessKeyBuilder.Build(row, ["DOCNO", "ADDTIME"]);

        Assert.Equal("D001|2026-04-08 10:11:12.1234567", businessKey);
    }

    /// <summary>
    /// 时间业务键组件应可稳定回解析为本地时间。
    /// </summary>
    [Fact]
    public void TryParseBusinessKeyDateTimeComponent_WhenValid_ShouldReturnLocalDateTime()
    {
        var success = SyncBusinessKeyBuilder.TryParseLocalDateTimeComponent(
            "2026-04-08 10:11:12.1234567",
            out var localDateTime);

        Assert.True(success);
        Assert.Equal(DateTimeKind.Local, localDateTime.Kind);
        Assert.Equal(new DateTime(2026, 4, 8, 10, 11, 12, 123, DateTimeKind.Local).AddTicks(4567), localDateTime);
    }

    /// <summary>
    /// 状态分表名称应按 TableCode+月份生成独立表名，格式为 sync_target_state_{tableCode}_{yyyyMM}。
    /// </summary>
    [Theory]
    [InlineData("WmsPickToWcs", "202604", "[dbo].[sync_target_state_WmsPickToWcs_202604]")]
    [InlineData("WmsSplitPickToLightCarton", "202512", "[dbo].[sync_target_state_WmsSplitPickToLightCarton_202512]")]
    [InlineData("SortingTaskTrace", "202601", "[dbo].[sync_target_state_SortingTaskTrace_202601]")]
    public void GetSyncStateTableFullName_ShouldGeneratePerTableCodeAndMonthName(string tableCode, string stateMonthToken, string expectedFullName)
    {
        var actualFullName = SqlServerSyncUpsertRepository.GetSyncStateTableFullName(tableCode, stateMonthToken);

        Assert.Equal(expectedFullName, actualFullName);
    }

    /// <summary>
    /// 状态分表名称对含非法字符的 TableCode 应抛出异常。
    /// </summary>
    [Fact]
    public void GetSyncStateTableFullName_WhenTableCodeContainsInvalidChar_ShouldThrow()
    {
        var action = () => SqlServerSyncUpsertRepository.GetSyncStateTableFullName("my-table; DROP TABLE--", "202604");

        Assert.Throws<InvalidOperationException>(action);
    }

    /// <summary>
    /// 状态分表名称对含非法月份标记的输入应抛出异常。
    /// </summary>
    [Theory]
    [InlineData("2026-04")]
    [InlineData("20260")]
    [InlineData("2026011")]
    [InlineData("")]
    [InlineData("2026AB")]
    [InlineData("202600")]
    [InlineData("202613")]
    public void GetSyncStateTableFullName_WhenStateMonthTokenInvalid_ShouldThrow(string stateMonthToken)
    {
        var action = () => SqlServerSyncUpsertRepository.GetSyncStateTableFullName("WmsPickToWcs", stateMonthToken);

        Assert.Throws<InvalidOperationException>(action);
    }

    /// <summary>
    /// 状态分表名称对空引用（null）月份标记输入应抛出异常。
    /// </summary>
    [Fact]
    public void GetSyncStateTableFullName_WhenStateMonthTokenIsNull_ShouldThrow()
    {
        var action = () => SqlServerSyncUpsertRepository.GetSyncStateTableFullName("WmsPickToWcs", null!);

        Assert.Throws<InvalidOperationException>(action);
    }

    /// <summary>
    /// 创建测试仓储。
    /// </summary>
    /// <returns>测试仓储。</returns>
    private static InMemorySqlServerSyncUpsertRepository CreateRepository()
    {
        var syncOptions = Options.Create(new SyncJobOptions
        {
            Tables =
            [
                new SyncTableOptions
                {
                    TableCode = "WmsSplitPickToLightCarton",
                    TargetLogicalTable = "IDX_PICKTOLIGHT_CARTON1",
                    UniqueKeys = ["CARTONNO"]
                }
            ]
        });
        var shardingOptions = Options.Create(new ShardingOptions());
        return new InMemorySqlServerSyncUpsertRepository(
            syncOptions,
            shardingOptions,
            new MonthShardSuffixResolver(),
            new NoOpShardTableProvisioner(),
            new PassThroughDangerZoneExecutor(),
            NullLogger<SqlServerSyncUpsertRepository>.Instance);
    }

    /// <summary>
    /// 构建测试请求。
    /// </summary>
    /// <param name="businessKey">业务键。</param>
    /// <param name="payload">摘要载荷。</param>
    /// <returns>合并请求。</returns>
    private static SyncMergeRequest CreateRequest(string businessKey, string payload)
    {
        return new SyncMergeRequest
        {
            TableCode = "WmsSplitPickToLightCarton",
            CursorColumn = "ADDTIME",
            UniqueKeys = ["CARTONNO"],
            Rows =
            [
                new Dictionary<string, object?>
                {
                    ["CARTONNO"] = businessKey,
                    ["PAYLOAD"] = payload,
                    ["ADDTIME"] = new DateTime(2026, 4, 7, 10, 0, 0, DateTimeKind.Local)
                }
            ]
        };
    }

    /// <summary>
    /// 构建单行测试请求（指定游标时间）。
    /// </summary>
    /// <param name="businessKey">业务键。</param>
    /// <param name="payload">摘要载荷。</param>
    /// <param name="addTime">游标时间。</param>
    /// <returns>合并请求。</returns>
    private static SyncMergeRequest CreateRequest(string businessKey, string payload, DateTime addTime)
    {
        return CreateRequest((businessKey, payload, addTime));
    }

    /// <summary>
    /// 构建多行测试请求。
    /// </summary>
    /// <param name="rows">业务行集合。</param>
    /// <returns>合并请求。</returns>
    private static SyncMergeRequest CreateRequest(params (string BusinessKey, string Payload, DateTime AddTime)[] rows)
    {
        return new SyncMergeRequest
        {
            TableCode = "WmsSplitPickToLightCarton",
            CursorColumn = "ADDTIME",
            UniqueKeys = ["CARTONNO"],
            Rows = rows.Select(row => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["CARTONNO"] = row.BusinessKey,
                ["PAYLOAD"] = row.Payload,
                ["ADDTIME"] = row.AddTime
            }).ToArray()
        };
    }
}
