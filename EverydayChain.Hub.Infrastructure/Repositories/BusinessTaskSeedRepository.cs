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
/// 定义当前类型。
/// </summary>
public sealed class BusinessTaskSeedRepository(
    IDbContextFactory<HubDbContext> dbContextFactory,
    IShardTableProvisioner shardTableProvisioner) : IBusinessTaskSeedRepository
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string BusinessTaskLogicalTable = "business_tasks";

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const string ManualSeedSourceTableCode = "manual_seed";

    private static readonly Regex TargetTableNameRegex = new("^business_tasks_(\\d{6})$", RegexOptions.Compiled);

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const int MaxInClauseBatchSize = 2000;

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
    /// 执行当前方法。
    /// </summary>
    public static IReadOnlyList<string> FilterInsertBarcodes(
        IReadOnlyList<string> candidateBarcodes,
        ISet<string> existingManualSeedBusinessKeys,
        out IReadOnlyList<string> skippedBarcodes)
    {
        // 步骤：按既定流程执行当前方法逻辑。
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

    private static string BuildTaskCode(string barcode, DateTime nowLocal, string requestNonce, int index)
    {
        var hash = ComputeFnv1aHash($"{barcode}|{nowLocal:yyyyMMddHHmmssfff}|{requestNonce}|{index}").ToString("x8");
        return $"manual_seed_{nowLocal:yyyyMMddHHmmssfff}_{requestNonce}_{hash}_{index:D4}";
    }

    private static uint ComputeFnv1aHash(string value)
    {
        /// <summary>
        /// 存储当前字段值。
        /// </summary>
        const uint offsetBasis = 2166136261;
        /// <summary>
        /// 存储当前字段值。
        /// </summary>
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
    /// 执行当前方法。
    /// </summary>
    private static async Task<List<string>> LoadExistingManualSeedKeysAsync(
        HubDbContext dbContext,
        IReadOnlyList<string> candidateBarcodes,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
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

    private static IReadOnlyList<IReadOnlyList<string>> SplitBarcodeBatches(IReadOnlyList<string> candidateBarcodes, int batchSize)
    {
        var result = new List<IReadOnlyList<string>>();
        if (candidateBarcodes.Count == 0)
        {
            return result;
        }

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
    /// 执行当前方法。
    /// </summary>
    private static BusinessTaskSeedResult BuildSuccessResult(
        string targetTableName,
        int insertedCount,
        int skippedExistingCount,
        IReadOnlyList<string> skippedBarcodes,
        IReadOnlyList<string> insertedBarcodes,
        string message)
    {
        // 步骤：按既定流程执行当前方法逻辑。
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

