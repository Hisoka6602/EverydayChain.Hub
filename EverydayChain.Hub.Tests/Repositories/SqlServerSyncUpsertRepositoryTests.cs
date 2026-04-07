using System.Text.Json;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EverydayChain.Hub.Tests.Services;

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
                    ["ADDTIME"] = DateTime.Now
                }
            ]
        };
    }

    /// <summary>
    /// 内存测试替身。
    /// </summary>
    private sealed class InMemorySqlServerSyncUpsertRepository(
        IOptions<SyncJobOptions> syncJobOptions,
        IOptions<ShardingOptions> shardingOptions,
        IShardSuffixResolver shardSuffixResolver,
        Infrastructure.Services.IDangerZoneExecutor dangerZoneExecutor,
        Microsoft.Extensions.Logging.ILogger<SqlServerSyncUpsertRepository> logger)
        : SqlServerSyncUpsertRepository(syncJobOptions, shardingOptions, shardSuffixResolver, dangerZoneExecutor, logger)
    {
        /// <summary>内存状态表。</summary>
        private readonly Dictionary<string, string> _states = new(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc/>
        protected override Task<SyncMergeResult> MergeCoreAsync(SyncMergeRequest request, CancellationToken ct)
        {
            var changedOperations = new Dictionary<string, SyncChangeOperationType>(StringComparer.OrdinalIgnoreCase);
            var result = new SyncMergeResult
            {
                ChangedOperations = changedOperations
            };
            foreach (var row in request.Rows)
            {
                var businessKey = row["CARTONNO"]?.ToString() ?? string.Empty;
                var digest = JsonSerializer.Serialize(row);
                if (!_states.TryGetValue(businessKey, out var existingDigest))
                {
                    _states[businessKey] = digest;
                    result.InsertCount++;
                    changedOperations[businessKey] = SyncChangeOperationType.Insert;
                    continue;
                }

                if (string.Equals(existingDigest, digest, StringComparison.Ordinal))
                {
                    result.SkipCount++;
                    continue;
                }

                _states[businessKey] = digest;
                result.UpdateCount++;
                changedOperations[businessKey] = SyncChangeOperationType.Update;
            }

            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        protected override Task<IReadOnlyList<SyncTargetStateRow>> ListTargetStateRowsCoreAsync(string tableCode, CancellationToken ct)
        {
            var rows = _states.Select(x => new SyncTargetStateRow
            {
                BusinessKey = x.Key,
                RowDigest = x.Value
            }).ToArray();
            return Task.FromResult<IReadOnlyList<SyncTargetStateRow>>(rows);
        }

        /// <inheritdoc/>
        protected override Task<int> DeleteByBusinessKeysCoreAsync(string tableCode, IReadOnlyList<string> businessKeys, DeletionPolicy deletionPolicy, CancellationToken ct)
        {
            var count = 0;
            foreach (var businessKey in businessKeys)
            {
                if (_states.Remove(businessKey))
                {
                    count++;
                }
            }

            return Task.FromResult(count);
        }
    }
}
