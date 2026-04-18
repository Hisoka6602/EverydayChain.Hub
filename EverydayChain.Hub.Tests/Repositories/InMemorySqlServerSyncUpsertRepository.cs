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
/// 内存版 SqlServerSyncUpsertRepository 测试替身。
/// </summary>
public sealed class InMemorySqlServerSyncUpsertRepository : SqlServerSyncUpsertRepository
{
    /// <summary>通过构造函数注入的分片后缀解析器，用于计算业务键所属分片后缀，确保测试替身与真实仓储使用相同的分片策略。</summary>
    private readonly IShardSuffixResolver _shardSuffixResolver;

    /// <summary>内存状态表。</summary>
    private readonly Dictionary<string, InMemoryState> _states = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>分片行集合（Key=Suffix|BusinessKey）。</summary>
    private readonly HashSet<string> _shardRows = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>分片迁移删除计数。</summary>
    public int ShardMigrationDeleteCount { get; private set; }

    /// <summary>
    /// 初始化内存版仓储测试替身。
    /// </summary>
    /// <param name="syncJobOptions">同步配置。</param>
    /// <param name="shardingOptions">分片配置。</param>
    /// <param name="shardSuffixResolver">分片后缀解析器。</param>
    /// <param name="shardTableProvisioner">分片建表服务。</param>
    /// <param name="dangerZoneExecutor">危险动作执行器。</param>
    /// <param name="logger">日志实例。</param>
    /// <remarks>步骤：保存注入的分片后缀解析器实例，确保测试替身与真实仓储采用相同分片策略。</remarks>
    public InMemorySqlServerSyncUpsertRepository(
        IOptions<SyncJobOptions> syncJobOptions,
        IOptions<ShardingOptions> shardingOptions,
        IShardSuffixResolver shardSuffixResolver,
        IShardTableProvisioner shardTableProvisioner,
        IDangerZoneExecutor dangerZoneExecutor,
        ILogger<SqlServerSyncUpsertRepository> logger)
        : base(syncJobOptions, shardingOptions, shardSuffixResolver, shardTableProvisioner, dangerZoneExecutor, logger)
    {
        _shardSuffixResolver = shardSuffixResolver;
    }

    /// <summary>
    /// 判断业务键是否位于指定分片。
    /// </summary>
    /// <param name="businessKey">业务键。</param>
    /// <param name="shardSuffix">分片后缀。</param>
    /// <returns>存在返回 true。</returns>
    public bool ExistsInShard(string businessKey, string shardSuffix)
    {
        return _shardRows.Contains($"{shardSuffix}|{businessKey}");
    }

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

    /// <inheritdoc/>
    protected override Task<IReadOnlyList<SyncTargetStateRow>> ListTargetStateRowsCoreAsync(string tableCode, CancellationToken ct)
    {
        var rows = _states.Select(x => new SyncTargetStateRow
        {
            BusinessKey = x.Key,
            RowDigest = x.Value.Digest
        }).ToArray();
        return Task.FromResult<IReadOnlyList<SyncTargetStateRow>>(rows);
    }

    /// <inheritdoc/>
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
    /// 内存状态行。
    /// </summary>
    /// <param name="Digest">摘要值。</param>
    /// <param name="ShardSuffix">分片后缀。</param>
    private readonly record struct InMemoryState(string Digest, string ShardSuffix);
}
