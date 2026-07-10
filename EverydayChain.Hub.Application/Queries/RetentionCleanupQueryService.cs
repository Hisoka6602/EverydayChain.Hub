using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Utilities;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Caching.Memory;

namespace EverydayChain.Hub.Application.Queries;

/// <summary>
/// 提供保留期清理审计查询服务。
/// </summary>
public sealed class RetentionCleanupQueryService : IRetentionCleanupQueryService
{
    /// <summary>
    /// 存储 CacheKeyDateTimeFormat 字段。
    /// </summary>
    private const string CacheKeyDateTimeFormat = "yyyyMMddHHmmssfffffff";

    /// <summary>
    /// 存储 NullCacheValue 字段。
    /// </summary>
    private const string NullCacheValue = "_";

    /// <summary>
    /// 存储 _retentionCleanupAuditLogRepository 字段。
    /// </summary>
    private readonly IRetentionCleanupAuditLogRepository _retentionCleanupAuditLogRepository;

    /// <summary>
    /// 存储 _memoryCache 字段。
    /// </summary>
    private readonly IMemoryCache _memoryCache;

    /// <summary>
    /// 存储 _queryCacheOptions 字段。
    /// </summary>
    private readonly QueryCacheOptions _queryCacheOptions;

    public RetentionCleanupQueryService(IRetentionCleanupAuditLogRepository retentionCleanupAuditLogRepository)
        : this(
            retentionCleanupAuditLogRepository,
            new MemoryCache(new MemoryCacheOptions()),
            new QueryCacheOptions())
    {
    }

    public RetentionCleanupQueryService(
        IRetentionCleanupAuditLogRepository retentionCleanupAuditLogRepository,
        IMemoryCache memoryCache,
        QueryCacheOptions queryCacheOptions)
    {
        _retentionCleanupAuditLogRepository = retentionCleanupAuditLogRepository;
        _memoryCache = memoryCache;
        _queryCacheOptions = queryCacheOptions;
    }

    /// <summary>
    /// 查询保留期清理审计记录。
    /// </summary>
    /// <param name="request">查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>分页查询结果。</returns>
    public async Task<RetentionCleanupAuditQueryResult> QueryAsync(RetentionCleanupAuditQueryRequest request, CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 50 : request.PageSize;
        var normalizedLogicalTableName = NormalizeOptionalValue(request.LogicalTableName);
        var normalizedTargetCode = NormalizeOptionalValue(request.TargetCode);
        var normalizedExecutionStage = NormalizeOptionalValue(request.ExecutionStage);
        var normalizedBatchId = NormalizeOptionalValue(request.BatchId);
        var emptyResult = new RetentionCleanupAuditQueryResult
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        if (request.EndTimeLocal <= request.StartTimeLocal)
        {
            return emptyResult;
        }

        if (_queryCacheOptions.Enabled)
        {
            var cacheKey = BuildCacheKey(
                request.StartTimeLocal,
                request.EndTimeLocal,
                normalizedLogicalTableName,
                normalizedTargetCode,
                normalizedExecutionStage,
                normalizedBatchId,
                pageNumber,
                pageSize);
            var ttlSeconds = Math.Clamp(_queryCacheOptions.RetentionCleanupSeconds, 1, 60);
            var cached = await MemoryCacheSingleFlight.GetOrCreateAsync(
                _memoryCache,
                cacheKey,
                TimeSpan.FromSeconds(ttlSeconds),
                _ => ExecuteQueryAsync(
                    request.StartTimeLocal,
                    request.EndTimeLocal,
                    normalizedLogicalTableName,
                    normalizedTargetCode,
                    normalizedExecutionStage,
                    normalizedBatchId,
                    pageNumber,
                    pageSize,
                    CancellationToken.None),
                cancellationToken);
            return cached ?? emptyResult;
        }

        return await ExecuteQueryAsync(
            request.StartTimeLocal,
            request.EndTimeLocal,
            normalizedLogicalTableName,
            normalizedTargetCode,
            normalizedExecutionStage,
            normalizedBatchId,
            pageNumber,
            pageSize,
            cancellationToken);
    }

    private async Task<RetentionCleanupAuditQueryResult> ExecuteQueryAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? logicalTableName,
        string? targetCode,
        string? executionStage,
        string? batchId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var queryResult = await _retentionCleanupAuditLogRepository.QueryAsync(
            startTimeLocal,
            endTimeLocal,
            logicalTableName,
            targetCode,
            executionStage,
            batchId,
            pageNumber,
            pageSize,
            cancellationToken);

        return new RetentionCleanupAuditQueryResult
        {
            TotalCount = queryResult.TotalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = queryResult.Items
                .Select(item => new RetentionCleanupAuditItem
                {
                    Id = item.Id,
                    BatchId = item.BatchId,
                    TargetCode = item.TargetCode,
                    LogicalTableName = item.LogicalTableName,
                    RetentionMode = item.RetentionMode,
                    TimeColumnName = item.TimeColumnName,
                    KeepMonths = item.KeepMonths,
                    IsDryRun = item.IsDryRun,
                    AllowDelete = item.AllowDelete,
                    ExecutionStage = item.ExecutionStage,
                    ScannedCount = item.ScannedCount,
                    CandidateCount = item.CandidateCount,
                    DeletedCount = item.DeletedCount,
                    Message = item.Message,
                    InstanceId = item.InstanceId,
                    ThresholdTimeLocal = item.ThresholdTimeLocal,
                    StartedTimeLocal = item.StartedTimeLocal,
                    CompletedTimeLocal = item.CompletedTimeLocal
                })
                .ToList()
        };
    }

    /// <summary>
    /// 规范化可选字符串筛选值。
    /// </summary>
    /// <param name="value">原始输入值。</param>
    /// <returns>去掉首尾空白后的值；若为空白则返回空引用。</returns>
    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private string BuildCacheKey(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? logicalTableName,
        string? targetCode,
        string? executionStage,
        string? batchId,
        int pageNumber,
        int pageSize)
    {
        var normalizedStartTime = QueryCacheTimeBucket.Normalize(startTimeLocal, _queryCacheOptions.AggregateTimeBucketSeconds);
        var normalizedEndTime = QueryCacheTimeBucket.Normalize(endTimeLocal, _queryCacheOptions.AggregateTimeBucketSeconds);
        return string.Join(':',
            "retention-cleanup",
            normalizedStartTime.ToString(CacheKeyDateTimeFormat),
            normalizedEndTime.ToString(CacheKeyDateTimeFormat),
            NormalizeCacheSegment(logicalTableName),
            NormalizeCacheSegment(targetCode),
            NormalizeCacheSegment(executionStage),
            NormalizeCacheSegment(batchId),
            pageNumber,
            pageSize);
    }

    private static string NormalizeCacheSegment(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? NullCacheValue
            : value.Trim();
    }
}
