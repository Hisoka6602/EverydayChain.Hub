using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace EverydayChain.Hub.Application.Queries;

/// <summary>
/// 定义 BusinessTaskReadService 类型。
/// </summary>
public sealed class BusinessTaskReadService : IBusinessTaskReadService
{
    /// <summary>
    /// 存储 _businessTaskRepository 字段。
    /// </summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;

    private readonly BusinessTaskQueryPolicy _queryPolicy = new();

    /// <summary>
    /// 存储 _logger 字段。
    /// </summary>
    private readonly ILogger<BusinessTaskReadService> _logger;

    /// <summary>
    /// 存储 LargeSkipWarningThreshold 字段。
    /// </summary>
    private const int LargeSkipWarningThreshold = 10_000;

    public BusinessTaskReadService(IBusinessTaskRepository businessTaskRepository)
        : this(businessTaskRepository, NullLogger<BusinessTaskReadService>.Instance)
    {
    }

    public BusinessTaskReadService(IBusinessTaskRepository businessTaskRepository, ILogger<BusinessTaskReadService> logger)
    {
        _businessTaskRepository = businessTaskRepository;
        _logger = logger;
    }

    public Task<BusinessTaskQueryResult> QueryTasksAsync(BusinessTaskQueryRequest request, CancellationToken cancellationToken)
    {
        return QueryCoreAsync(request, false, false, cancellationToken);
    }

    public Task<BusinessTaskQueryResult> QueryExceptionsAsync(BusinessTaskQueryRequest request, CancellationToken cancellationToken)
    {
        return QueryCoreAsync(request, true, false, cancellationToken);
    }

    public Task<BusinessTaskQueryResult> QueryRecirculationsAsync(BusinessTaskQueryRequest request, CancellationToken cancellationToken)
    {
        return QueryCoreAsync(request, false, true, cancellationToken);
    }

    /// <summary>
    /// 执行 QueryCoreAsync 方法。
    /// </summary>
    private async Task<BusinessTaskQueryResult> QueryCoreAsync(
        BusinessTaskQueryRequest request,
        bool onlyException,
        bool onlyRecirculation,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 QueryCoreAsync 方法的核心处理流程。
        if (request.EndTimeLocal <= request.StartTimeLocal)
        {
            return new BusinessTaskQueryResult
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        var filter = new BusinessTaskSearchFilter
        {
            StartTimeLocal = request.StartTimeLocal,
            EndTimeLocal = request.EndTimeLocal,
            WaveCode = NormalizeOptionalValue(request.WaveCode),
            Barcode = NormalizeOptionalValue(request.Barcode),
            DockCode = NormalizeOptionalValue(request.DockCode),
            ChuteCode = NormalizeOptionalValue(request.ChuteCode),
            OnlyException = onlyException,
            OnlyRecirculation = onlyRecirculation
        };
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 50 : request.PageSize;
        if (request.LastCreatedTimeLocal.HasValue && request.LastId.HasValue)
        {
            var cursorRows = await _businessTaskRepository.QueryByCursorConditionsAsync(
                filter,
                request.LastCreatedTimeLocal,
                request.LastId,
                pageSize + 1,
                cancellationToken);
            var hasMore = cursorRows.Count > pageSize;
            var pageRows = hasMore ? cursorRows.Take(pageSize).ToList() : cursorRows.ToList();
            var nextCursor = hasMore ? pageRows.Last() : null;
            return new BusinessTaskQueryResult
            {
                TotalCount = -1,
                PageNumber = pageNumber,
                PageSize = pageSize,
                HasMore = hasMore,
                NextLastCreatedTimeLocal = nextCursor?.CreatedTimeLocal,
                NextLastId = nextCursor?.Id,
                PaginationMode = "Cursor",
                Items = pageRows
                    .Select(MapItem)
                    .ToList()
            };
        }

        var skip = (pageNumber - 1) * pageSize;
        var stopwatch = Stopwatch.StartNew();
        var pageResult = await _businessTaskRepository.QueryPageWithTotalCountByConditionsAsync(filter, skip, pageSize, cancellationToken);
        stopwatch.Stop();
        var items = pageResult.Items.Select(MapItem).ToList();
        if (skip >= LargeSkipWarningThreshold)
        {
            _logger.LogWarning(
                "检测到大页码分页查询，建议改用游标分页（传入 LastCreatedTimeLocal 与 LastId）。Skip={Skip}, PageNumber={PageNumber}, PageSize={PageSize}, ElapsedMilliseconds={ElapsedMilliseconds}",
                skip,
                pageNumber,
                pageSize,
                stopwatch.ElapsedMilliseconds);
        }

        return new BusinessTaskQueryResult
        {
            TotalCount = pageResult.TotalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            HasMore = skip + items.Count < pageResult.TotalCount,
            PaginationMode = "PageNumber",
            Items = items
        };
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private BusinessTaskQueryItem MapItem(BusinessTaskEntity task)
    {
        return new BusinessTaskQueryItem
        {
            TaskCode = task.TaskCode,
            Barcode = task.Barcode,
            WaveCode = task.WaveCode,
            SourceType = task.SourceType,
            Status = task.Status,
            TargetChuteCode = task.TargetChuteCode,
            ActualChuteCode = task.ActualChuteCode,
            DockCode = _queryPolicy.ResolveDockCode(task),
            IsRecirculated = task.IsRecirculated,
            IsException = task.IsException || task.Status == BusinessTaskStatus.Exception,
            CreatedTimeLocal = task.CreatedTimeLocal,
            OrderId = task.OrderId,
            StoreId = task.StoreId,
            StoreName = task.StoreName,
            ProductCode = task.ProductCode,
            PickLocation = task.PickLocation
        };
    }
}

