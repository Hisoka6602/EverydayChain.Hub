using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
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

        var existingManualSeedKeys = await dbContext.BusinessTasks
            .AsNoTracking()
            .Where(task => task.SourceTableCode == ManualSeedSourceTableCode && candidateBarcodes.Contains(task.BusinessKey))
            .Select(task => task.BusinessKey)
            .ToListAsync(cancellationToken);
        var existingManualSeedKeySet = existingManualSeedKeys.ToHashSet(StringComparer.Ordinal);
        var insertBarcodes = FilterInsertBarcodes(candidateBarcodes, existingManualSeedKeySet, out var skippedExistingCount);
        if (insertBarcodes.Count == 0)
        {
            return BuildSuccessResult(targetTableName, 0, skippedExistingCount, "模拟补数执行完成，未新增数据。");
        }

        var nowLocal = DateTime.Now;
        var entities = new List<BusinessTaskEntity>(insertBarcodes.Count);
        for (var index = 0; index < insertBarcodes.Count; index++)
        {
            entities.Add(BuildManualSeedEntity(insertBarcodes[index], nowLocal, index));
        }

        dbContext.BusinessTasks.AddRange(entities);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return BuildSuccessResult(targetTableName, entities.Count, skippedExistingCount, "模拟补数写入成功。");
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
    /// <param name="skippedExistingCount">已存在跳过数量。</param>
    /// <returns>可插入条码集合。</returns>
    public static IReadOnlyList<string> FilterInsertBarcodes(
        IReadOnlyList<string> candidateBarcodes,
        ISet<string> existingManualSeedBusinessKeys,
        out int skippedExistingCount)
    {
        skippedExistingCount = 0;
        var insertBarcodes = new List<string>(candidateBarcodes.Count);
        foreach (var barcode in candidateBarcodes)
        {
            if (existingManualSeedBusinessKeys.Contains(barcode))
            {
                skippedExistingCount++;
                continue;
            }

            insertBarcodes.Add(barcode);
        }

        return insertBarcodes;
    }

    /// <summary>
    /// 构建模拟补数业务任务实体。
    /// </summary>
    /// <param name="barcode">条码。</param>
    /// <param name="nowLocal">当前本地时间。</param>
    /// <param name="index">批次内序号。</param>
    /// <returns>业务任务实体。</returns>
    private static BusinessTaskEntity BuildManualSeedEntity(string barcode, DateTime nowLocal, int index)
    {
        var entity = new BusinessTaskEntity
        {
            TaskCode = BuildTaskCode(barcode, nowLocal, index),
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
    /// <param name="index">批次内序号。</param>
    /// <returns>任务编码。</returns>
    private static string BuildTaskCode(string barcode, DateTime nowLocal, int index)
    {
        var hash = ComputeFnv1aHash($"{barcode}|{nowLocal:yyyyMMddHHmmssfff}|{index}").ToString("x8");
        return $"manual_seed_{nowLocal:yyyyMMddHHmmssfff}_{hash}_{index:D4}";
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
            TargetTableName = targetTableName
        };
    }

    /// <summary>
    /// 构建成功结果。
    /// </summary>
    /// <param name="targetTableName">目标表名。</param>
    /// <param name="insertedCount">插入数量。</param>
    /// <param name="skippedExistingCount">已存在跳过数量。</param>
    /// <param name="message">结果消息。</param>
    /// <returns>成功结果。</returns>
    private static BusinessTaskSeedResult BuildSuccessResult(string targetTableName, int insertedCount, int skippedExistingCount, string message)
    {
        return new BusinessTaskSeedResult
        {
            IsSuccess = true,
            Message = message,
            TargetTableName = targetTableName,
            InsertedCount = insertedCount,
            SkippedExistingCount = skippedExistingCount
        };
    }
}
