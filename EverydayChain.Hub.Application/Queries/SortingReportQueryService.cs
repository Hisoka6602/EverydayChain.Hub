using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Caching.Memory;
using System.Text;

namespace EverydayChain.Hub.Application.Queries;

/// <summary>
/// 分拣报表查询服务实现。
/// </summary>
public sealed class SortingReportQueryService : ISortingReportQueryService
{
    /// <summary>
    /// 业务任务仓储。
    /// </summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;

    /// <summary>
    /// 业务任务统计规则。
    /// </summary>
    private readonly BusinessTaskQueryPolicy _queryPolicy = new();

    /// <summary>
    /// 内存缓存。
    /// </summary>
    private readonly IMemoryCache _memoryCache;

    /// <summary>
    /// 查询缓存配置。
    /// </summary>
    private readonly QueryCacheOptions _queryCacheOptions;

    /// <summary>
    /// 初始化分拣报表查询服务。
    /// </summary>
    /// <param name="businessTaskRepository">业务任务仓储。</param>
    public SortingReportQueryService(IBusinessTaskRepository businessTaskRepository)
        : this(businessTaskRepository, new MemoryCache(new MemoryCacheOptions()), new QueryCacheOptions())
    {
    }

    /// <summary>
    /// 初始化分拣报表查询服务。
    /// </summary>
    /// <param name="businessTaskRepository">业务任务仓储。</param>
    /// <param name="memoryCache">内存缓存。</param>
    /// <param name="queryCacheOptions">缓存配置。</param>
    public SortingReportQueryService(
        IBusinessTaskRepository businessTaskRepository,
        IMemoryCache memoryCache,
        QueryCacheOptions queryCacheOptions)
    {
        _businessTaskRepository = businessTaskRepository;
        _memoryCache = memoryCache;
        _queryCacheOptions = queryCacheOptions;
    }

    /// <inheritdoc/>
    public async Task<SortingReportQueryResult> QueryAsync(SortingReportQueryRequest request, CancellationToken cancellationToken)
    {
        // 步骤 1：校验时间区间。
        if (request.EndTimeLocal <= request.StartTimeLocal)
        {
            return new SortingReportQueryResult
            {
                StartTimeLocal = request.StartTimeLocal,
                EndTimeLocal = request.EndTimeLocal
            };
        }

        var selectedDockCode = string.IsNullOrWhiteSpace(request.DockCode) ? null : request.DockCode.Trim();
        var cacheKey = $"sorting-report:{request.StartTimeLocal:yyyyMMddHHmmssfffffff}:{request.EndTimeLocal:yyyyMMddHHmmssfffffff}:{selectedDockCode}";
        if (_queryCacheOptions.Enabled)
        {
            var ttl = Math.Clamp(_queryCacheOptions.SortingReportSeconds, 1, 120);
            var cached = await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttl);
                return await BuildResultAsync(request.StartTimeLocal, request.EndTimeLocal, selectedDockCode, cancellationToken);
            });
            return cached ?? new SortingReportQueryResult
            {
                StartTimeLocal = request.StartTimeLocal,
                EndTimeLocal = request.EndTimeLocal
            };
        }

        return await BuildResultAsync(request.StartTimeLocal, request.EndTimeLocal, selectedDockCode, cancellationToken);
    }

    /// <summary>
    /// 构建报表查询结果。
    /// </summary>
    /// <param name="startTimeLocal">开始时间。</param>
    /// <param name="endTimeLocal">结束时间。</param>
    /// <param name="selectedDockCode">选中码头。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>报表结果。</returns>
    private async Task<SortingReportQueryResult> BuildResultAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string? selectedDockCode,
        CancellationToken cancellationToken)
    {
        // 步骤 1：在仓储侧执行码头维度筛选与聚合。
        var dockRows = await _businessTaskRepository.AggregateDockDashboardAsync(
            startTimeLocal,
            endTimeLocal,
            null,
            selectedDockCode,
            cancellationToken);
        var rows = dockRows
            .Select(row => new SortingReportRow
            {
                DockCode = row.DockCode,
                SplitTotalCount = row.SplitTotalCount,
                FullCaseTotalCount = row.FullCaseTotalCount,
                SplitSortedCount = row.SplitSortedCount,
                FullCaseSortedCount = row.FullCaseSortedCount,
                RecirculatedCount = row.RecirculatedCount,
                ExceptionCount = _queryPolicy.IsDockSeven(row.DockCode) ? row.ExceptionCount : 0
            })
            .OrderBy(row => row.DockCode, StringComparer.Ordinal)
            .ToList();

        // 步骤 3：返回报表结果。
        return new SortingReportQueryResult
        {
            StartTimeLocal = startTimeLocal,
            EndTimeLocal = endTimeLocal,
            SelectedDockCode = selectedDockCode,
            Rows = rows
        };
    }

    /// <inheritdoc/>
    public async Task<string> ExportCsvAsync(SortingReportQueryRequest request, CancellationToken cancellationToken)
    {
        // 步骤 1：复用查询结果，确保导出与查询口径一致。
        var result = await QueryAsync(request, cancellationToken);

        // 步骤 2：生成 CSV 文本。
        var builder = new StringBuilder();
        builder.AppendLine("码头号,拆零总数,整件总数,拆零分拣数,整件分拣数,回流数,异常数");
        foreach (var row in result.Rows)
        {
            builder.AppendLine($"{EscapeCsvField(row.DockCode)},{row.SplitTotalCount},{row.FullCaseTotalCount},{row.SplitSortedCount},{row.FullCaseSortedCount},{row.RecirculatedCount},{row.ExceptionCount}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// CSV 字段转义。
    /// </summary>
    /// <param name="value">原始字段值。</param>
    /// <returns>转义后字段。</returns>
    private static string EscapeCsvField(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
