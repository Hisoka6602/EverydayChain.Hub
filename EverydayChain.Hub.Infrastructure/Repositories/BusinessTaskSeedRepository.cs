using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 业务任务模拟补数仓储实现。
/// </summary>
public sealed class BusinessTaskSeedRepository(
    IDbContextFactory<HubDbContext> dbContextFactory,
    IShardTableProvisioner shardTableProvisioner) : IBusinessTaskSeedRepository
{
    /// <summary>
    /// 业务任务逻辑表名。
    /// </summary>
    private const string BusinessTaskLogicalTable = "business_tasks";

    /// <summary>
    /// 模拟补数来源表编码。
    /// </summary>
    private const string ManualSeedSourceTableCode = "manual_seed";

    /// <summary>
    /// 目标表名匹配正则。
    /// </summary>
    private static readonly Regex TargetTableNameRegex = new("^business_tasks_(\\d{6})$", RegexOptions.Compiled);

    /// <summary>
    /// 单次 IN 查询最大参数数量。
    /// </summary>
    private const int MaxInClauseBatchSize = 2000;

    /// <inheritdoc/>
    public async Task<BusinessTaskSeedResult> InsertManualSeedAsync(BusinessTaskSeedCommand command, CancellationToken cancellationToken)
    {
        if (command is null)
        {
            return BusinessTaskSeedResult.Fail("模拟补数请求不能为空。");
        }

        if (!TryParseTargetTableName(command.TargetTableName, out var targetTableName, out var suffix))
        {
            return BuildFailResult("目标表名非法，仅允许 business_tasks_yyyyMM 格式。", command.TargetTableName);
        }

        var candidateBarcodes = command.Barcodes ?? [];
        if (candidateBarcodes.Count == 0)
        {
            return BuildFailResult("条码集合不能为空。", targetTableName);
        }

        await shardTableProvisioner.EnsureShardTableAsync(BusinessTaskLogicalTable, suffix, cancellationToken);
        using var scope = TableSuffixScope.Use(suffix);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var existingManualSeedKeys = await LoadExistingManualSeedKeysAsync(dbContext, candidateBarcodes, cancellationToken);
        var existingManualSeedKeySet = existingManualSeedKeys.ToHashSet(StringComparer.Ordinal);
        var insertBarcodes = FilterInsertBarcodes(candidateBarcodes, existingManualSeedKeySet, out var skippedBarcodes);
        if (insertBarcodes.Count == 0)
        {
            return BuildSuccessResult(targetTableName, 0, skippedBarcodes.Count, skippedBarcodes, insertBarcodes, "模拟补数执行完成，未新增数据。");
        }

        var nowLocal = DateTime.Now;
        // 请求级随机因子：用于降低并发场景下同毫秒 TaskCode 冲突风险。
        var requestNonce = RandomNumberGenerator.GetHexString(8).ToLowerInvariant();
        var entities = new List<BusinessTaskEntity>(insertBarcodes.Count);
        for (var index = 0; index < insertBarcodes.Count; index++)
        {
            entities.Add(BuildManualSeedEntity(insertBarcodes[index], nowLocal, requestNonce, index));
        }

        dbContext.BusinessTasks.AddRange(entities);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return BuildSuccessResult(targetTableName, entities.Count, skippedBarcodes.Count, skippedBarcodes, insertBarcodes, "模拟补数写入成功。");
    }

    /// <summary>
    /// 校验并解析目标表名。
    /// </summary>
    /// <param name="targetTableName">目标表名。</param>
    /// <param name="normalizedTargetTableName">规范化目标表名。</param>
    /// <param name="suffix">解析出的分表后缀。</param>
    /// <returns>解析成功返回 true，否则返回 false。</returns>
    public static bool TryParseTargetTableName(string targetTableName, out string normalizedTargetTableName, out string suffix)
    {
        normalizedTargetTableName = string.Empty;
        suffix = string.Empty;
        if (string.IsNullOrWhiteSpace(targetTableName))
        {
            return false;
        }

        var trimmedTargetTableName = targetTableName.Trim();
        var match = TargetTableNameRegex.Match(trimmedTargetTableName);
        if (!match.Success)
        {
            return false;
        }

        var monthToken = match.Groups[1].Value;
        if (!DateTime.TryParseExact(
                monthToken + "01",
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out _))
        {
            return false;
        }

        normalizedTargetTableName = trimmedTargetTableName;
        suffix = $"_{monthToken}";
        return true;
    }

    /// <summary>
    /// 根据目标表已存在模拟数据键过滤可插入条码。
    /// </summary>
    /// <param name="candidateBarcodes">候选条码集合。</param>
    /// <param name="existingManualSeedBusinessKeys">目标表内已存在模拟数据业务键集合。</param>
    /// <param name="skippedBarcodes">已存在跳过条码集合。</param>
    /// <returns>可插入条码集合。</returns>
    public static IReadOnlyList<string> FilterInsertBarcodes(
        IReadOnlyList<string> candidateBarcodes,
        ISet<string> existingManualSeedBusinessKeys,
        out IReadOnlyList<string> skippedBarcodes)
    {
        var insertBarcodes = new List<string>(candidateBarcodes.Count);
        var skippedBarcodesList = new List<string>(candidateBarcodes.Count);
        foreach (var barcode in candidateBarcodes)
        {
            if (existingManualSeedBusinessKeys.Contains(barcode))
            {
                skippedBarcodesList.Add(barcode);
                continue;
            }

            insertBarcodes.Add(barcode);
        }

        skippedBarcodes = skippedBarcodesList;
        return insertBarcodes;
    }

    /// <summary>
    /// 构建模拟补数业务任务实体。
    /// </summary>
    /// <param name="barcode">条码。</param>
    /// <param name="nowLocal">当前本地时间。</param>
    /// <param name="requestNonce">请求级随机因子。</param>
    /// <param name="index">批次内序号。</param>
    /// <returns>业务任务实体。</returns>
    private static BusinessTaskEntity BuildManualSeedEntity(string barcode, DateTime nowLocal, string requestNonce, int index)
    {
        var entity = new BusinessTaskEntity
        {
            TaskCode = BuildTaskCode(barcode, nowLocal, requestNonce, index),
            SourceTableCode = ManualSeedSourceTableCode,
            SourceType = BusinessTaskSourceType.Unknown,
            BusinessKey = barcode,
            Barcode = barcode,
            NormalizedBarcode = barcode,
            Status = BusinessTaskStatus.Created,
            FeedbackStatus = BusinessTaskFeedbackStatus.NotRequired,
            CreatedTimeLocal = nowLocal,
            UpdatedTimeLocal = nowLocal
        };
        entity.RefreshQueryFields();
        return entity;
    }

    /// <summary>
    /// 构建模拟补数任务编码。
    /// </summary>
    /// <param name="barcode">条码。</param>
    /// <param name="nowLocal">当前本地时间。</param>
    /// <param name="requestNonce">请求级随机因子。</param>
    /// <param name="index">批次内序号。</param>
    /// <returns>任务编码。</returns>
    private static string BuildTaskCode(string barcode, DateTime nowLocal, string requestNonce, int index)
    {
        var hash = ComputeFnv1aHash($"{barcode}|{nowLocal:yyyyMMddHHmmssfff}|{requestNonce}|{index}").ToString("x8");
        return $"manual_seed_{nowLocal:yyyyMMddHHmmssfff}_{requestNonce}_{hash}_{index:D4}";
    }

    /// <summary>
    /// 计算 FNV-1a 32 位哈希值。
    /// </summary>
    /// <param name="value">原始值。</param>
    /// <returns>哈希值。</returns>
    private static uint ComputeFnv1aHash(string value)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;
        var hash = offsetBasis;
        foreach (var character in value)
        {
            hash ^= character;
            hash *= prime;
        }

        return hash;
    }

    /// <summary>
    /// 分批加载目标表内已存在的模拟补数业务键。
    /// </summary>
    /// <param name="dbContext">数据库上下文。</param>
    /// <param name="candidateBarcodes">候选条码集合。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已存在业务键集合。</returns>
    private static async Task<List<string>> LoadExistingManualSeedKeysAsync(
        HubDbContext dbContext,
        IReadOnlyList<string> candidateBarcodes,
        CancellationToken cancellationToken)
    {
        // 聚合所有批次查询命中的已存在业务键。
        var existingManualSeedKeys = new List<string>(candidateBarcodes.Count);
        foreach (var barcodeBatch in SplitBarcodeBatches(candidateBarcodes, MaxInClauseBatchSize))
        {
            var existingBatchKeys = await dbContext.BusinessTasks
                .AsNoTracking()
                .Where(task => task.SourceTableCode == ManualSeedSourceTableCode && barcodeBatch.Contains(task.BusinessKey))
                .Select(task => task.BusinessKey)
                .ToListAsync(cancellationToken);
            existingManualSeedKeys.AddRange(existingBatchKeys);
        }

        return existingManualSeedKeys;
    }

    /// <summary>
    /// 将候选条码按指定批次大小分组。
    /// </summary>
    /// <param name="candidateBarcodes">候选条码集合。</param>
    /// <param name="batchSize">批次大小。</param>
    /// <returns>分批结果集合，每批大小不超过 <paramref name="batchSize"/>，最后一批可能不足一个批次。</returns>
    private static IReadOnlyList<IReadOnlyList<string>> SplitBarcodeBatches(IReadOnlyList<string> candidateBarcodes, int batchSize)
    {
        var result = new List<IReadOnlyList<string>>();
        if (candidateBarcodes.Count == 0)
        {
            return result;
        }

        // 防御性兜底：当批次大小小于 1 时按 1 处理，避免循环步长为 0 导致死循环。
        var actualBatchSize = Math.Max(1, batchSize);
        for (var index = 0; index < candidateBarcodes.Count; index += actualBatchSize)
        {
            var count = Math.Min(actualBatchSize, candidateBarcodes.Count - index);
            var batch = candidateBarcodes
                .Skip(index)
                .Take(count)
                .ToList();
            result.Add(batch);
        }

        return result;
    }

    /// <summary>
    /// 构建失败结果。
    /// </summary>
    /// <param name="message">失败消息。</param>
    /// <param name="targetTableName">目标表名。</param>
    /// <returns>失败结果。</returns>
    private static BusinessTaskSeedResult BuildFailResult(string message, string targetTableName)
    {
        return new BusinessTaskSeedResult
        {
            IsSuccess = false,
            Message = message,
            TargetTableName = targetTableName,
            InsertedBarcodes = [],
            SkippedBarcodes = []
        };
    }

    /// <summary>
    /// 构建成功结果。
    /// </summary>
    /// <param name="targetTableName">目标表名。</param>
    /// <param name="insertedCount">插入数量。</param>
    /// <param name="skippedExistingCount">已存在跳过数量。</param>
    /// <param name="skippedBarcodes">已存在跳过条码集合。</param>
    /// <param name="insertedBarcodes">成功插入条码集合。</param>
    /// <param name="message">结果消息。</param>
    /// <returns>成功结果。</returns>
    private static BusinessTaskSeedResult BuildSuccessResult(
        string targetTableName,
        int insertedCount,
        int skippedExistingCount,
        IReadOnlyList<string> skippedBarcodes,
        IReadOnlyList<string> insertedBarcodes,
        string message)
    {
        return new BusinessTaskSeedResult
        {
            IsSuccess = true,
            Message = message,
            TargetTableName = targetTableName,
            InsertedCount = insertedCount,
            SkippedExistingCount = skippedExistingCount,
            InsertedBarcodes = insertedBarcodes,
            SkippedBarcodes = skippedBarcodes
        };
    }
}
