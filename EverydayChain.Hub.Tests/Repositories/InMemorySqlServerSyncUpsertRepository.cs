using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Repositories;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace EverydayChain.Hub.Tests.Repositories;

/// <summary>
/// 定义 InMemorySqlServerSyncUpsertRepository 类型。
/// </summary>
public sealed class InMemorySqlServerSyncUpsertRepository : SqlServerSyncUpsertRepository
{
    /// <summary>
    /// 存储 _shardSuffixResolver 字段。
    /// </summary>
    private readonly IShardSuffixResolver _shardSuffixResolver;

    /// <summary>
    /// 按业务键保存内存版同步目标状态。
    /// </summary>
    private readonly Dictionary<string, InMemoryState> _states = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 保存测试中已写入分表的业务键集合。
    /// </summary>
    private readonly HashSet<string> _shardRows = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 获取或设置 ShardMigrationDeleteCount。
    /// </summary>
    public int ShardMigrationDeleteCount { get; private set; }

    /// <summary>
    /// 执行 InMemorySqlServerSyncUpsertRepository 方法。
    /// </summary>
    public InMemorySqlServerSyncUpsertRepository(
        IOptions<SyncJobOptions> syncJobOptions,
        IOptions<ShardingOptions> shardingOptions,
        IShardSuffixResolver shardSuffixResolver,
        IShardTableProvisioner shardTableProvisioner,
        IDangerZoneExecutor dangerZoneExecutor,
        ILogger<SqlServerSyncUpsertRepository> logger)
        : base(syncJobOptions, shardingOptions, shardSuffixResolver, shardTableProvisioner, dangerZoneExecutor, logger)
    {
        // 步骤：执行 base 方法的核心处理流程。
        _shardSuffixResolver = shardSuffixResolver;
    }

    public bool ExistsInShard(string businessKey, string shardSuffix)
    {
        return _shardRows.Contains($"{shardSuffix}|{businessKey}");
    }

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
            var digest = JsonConvert.SerializeObject(row);
            var addTime = row.TryGetValue("ADDTIME", out var addTimeValue) && addTimeValue is DateTime dateTime
                ? dateTime
                : DateTime.Now;
            var shardSuffix = _shardSuffixResolver.Resolve(new DateTimeOffset(addTime));
            if (!_states.TryGetValue(businessKey, out var existingState))
            {
                _states[businessKey] = new InMemoryState(digest, shardSuffix);
                _shardRows.Add($"{shardSuffix}|{businessKey}");
                result.InsertCount++;
                changedOperations[businessKey] = SyncChangeOperationType.Insert;
                continue;
            }

            if (string.Equals(existingState.Digest, digest, StringComparison.Ordinal)
                && string.Equals(existingState.ShardSuffix, shardSuffix, StringComparison.OrdinalIgnoreCase))
            {
                result.SkipCount++;
                continue;
            }

            if (!string.Equals(existingState.ShardSuffix, shardSuffix, StringComparison.OrdinalIgnoreCase))
            {
                _shardRows.Remove($"{existingState.ShardSuffix}|{businessKey}");
                ShardMigrationDeleteCount++;
            }

            _states[businessKey] = new InMemoryState(digest, shardSuffix);
            _shardRows.Add($"{shardSuffix}|{businessKey}");
            result.UpdateCount++;
            changedOperations[businessKey] = SyncChangeOperationType.Update;
        }

        return Task.FromResult(result);
    }

    protected override Task<IReadOnlyList<SyncTargetStateRow>> ListTargetStateRowsCoreAsync(string tableCode, CancellationToken ct)
    {
        var rows = _states.Select(x => new SyncTargetStateRow
        {
            BusinessKey = x.Key,
            RowDigest = x.Value.Digest
        }).ToArray();
        return Task.FromResult<IReadOnlyList<SyncTargetStateRow>>(rows);
    }

    protected override Task<int> DeleteByBusinessKeysCoreAsync(string tableCode, IReadOnlyList<string> businessKeys, DeletionPolicy deletionPolicy, CancellationToken ct)
    {
        var count = 0;
        foreach (var businessKey in businessKeys)
        {
            if (_states.Remove(businessKey, out var state))
            {
                _shardRows.Remove($"{state.ShardSuffix}|{businessKey}");
                count++;
            }
        }

        return Task.FromResult(count);
    }

    /// <summary>
    /// 定义 InMemoryState 类型。
    /// </summary>
    private readonly record struct InMemoryState(string Digest, string ShardSuffix);
}


